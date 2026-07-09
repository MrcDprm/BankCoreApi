using System.Security.Claims;
using BankCoreApi.Controllers.Dtos;
using BankCoreApi.Data;
using BankCoreApi.Helpers;
using BankCoreApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankCoreApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TransferController : ControllerBase
{
    private const string TransferAciklama = "Para Transferi";
    private const string UcretAciklama = "Transfer Ücreti ve BSMV";

    private readonly BankaDbContext _context;
    private readonly ITotpServis _totpServis;
    private readonly ITransferServis _transferServis;

    public TransferController(ITransferServis transferServis, ITotpServis totpServis, BankaDbContext context)
    {
        _transferServis = transferServis;
        _totpServis = totpServis;
        _context = context;
    }

    [HttpGet("dekont/{islemGrupId:guid}")]
    public async Task<IActionResult> DekontGetir([FromRoute] Guid islemGrupId)
    {
        var sonuc = await DekontOlusturAsync(islemGrupId);

        if (sonuc.Hata is not null)
        {
            return sonuc.Hata;
        }

        return Ok(sonuc.Dekont);
    }

    [HttpGet("dekont/{islemGrupId:guid}/pdf")]
    public async Task<IActionResult> DekontPdf([FromRoute] Guid islemGrupId)
    {
        var sonuc = await DekontOlusturAsync(islemGrupId);

        if (sonuc.Hata is not null)
        {
            return sonuc.Hata;
        }

        var pdfBytes = PdfHelper.DekontOlustur(sonuc.Dekont!);
        return File(pdfBytes, "application/pdf", "Dekont.pdf");
    }

    private async Task<(DekontResponse? Dekont, IActionResult? Hata)> DekontOlusturAsync(Guid islemGrupId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userId, out var hesapId))
        {
            return (null, Unauthorized());
        }

        var kayitlar = await _context.DefterKayitlar
            .Where(d => d.IslemGrupId == islemGrupId)
            .ToListAsync();

        if (kayitlar.Count == 0)
        {
            return (null, NotFound(new { mesaj = "Dekont bulunamadı." }));
        }

        var transferKayitlari = kayitlar
            .Where(d => d.Aciklama == TransferAciklama)
            .ToList();

        var gonderenKayit = transferKayitlari.FirstOrDefault(d => d.Amount < 0);
        var aliciKayit = transferKayitlari.FirstOrDefault(d => d.Amount > 0);

        if (gonderenKayit is null || aliciKayit is null)
        {
            return (null, NotFound(new { mesaj = "Transfer dekontu bulunamadı." }));
        }

        if (hesapId != gonderenKayit.HesapId && hesapId != aliciKayit.HesapId)
        {
            return (null, Forbid());
        }

        var hesapIdleri = new[] { gonderenKayit.HesapId, aliciKayit.HesapId };
        var hesaplar = await _context.Hesaplar
            .Where(h => hesapIdleri.Contains(h.Id))
            .ToDictionaryAsync(h => h.Id);

        if (!hesaplar.TryGetValue(gonderenKayit.HesapId, out var gonderenHesap) ||
            !hesaplar.TryGetValue(aliciKayit.HesapId, out var aliciHesap))
        {
            return (null, NotFound(new { mesaj = "Hesap bilgileri bulunamadı." }));
        }

        var ucretKayit = kayitlar.FirstOrDefault(d => d.Aciklama == UcretAciklama);
        var bsmvKesintisi = ucretKayit is null ? 0m : Math.Abs(ucretKayit.Amount);

        var dekont = new DekontResponse(
            islemGrupId,
            gonderenHesap.HesapSahibiAd,
            gonderenHesap.HesapNo,
            aliciHesap.HesapSahibiAd,
            aliciHesap.HesapNo,
            aliciKayit.Amount,
            bsmvKesintisi,
            gonderenKayit.CreatedAt);

        return (dekont, null);
    }

    [HttpPost]
    public async Task<IActionResult> TransferYap([FromBody] TransferIstek istek)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userId, out var gonderenHesapId))
        {
            return Unauthorized();
        }

        if (istek.Miktar <= 0)
        {
            return BadRequest(new { mesaj = "Transfer miktari 0'dan buyuk olmalidir." });
        }

        if (string.IsNullOrWhiteSpace(istek.AliciHesapNo))
        {
            return BadRequest(new { mesaj = "Alici hesap numarasi zorunludur." });
        }

        var gonderenHesap = await _context.Hesaplar.FirstOrDefaultAsync(h => h.Id == gonderenHesapId);

        if (gonderenHesap is null)
        {
            return BadRequest(new { mesaj = "Gonderen hesap bulunamadi." });
        }

        if (string.IsNullOrWhiteSpace(gonderenHesap.TotpSecretKey))
        {
            return BadRequest(new { mesaj = "Bu hesap icin 2FA aktif degil. Once TOTP kurun." });
        }

        if (!_totpServis.KoduDogrula(gonderenHesap.TotpSecretKey, istek.TotpKodu))
        {
            return BadRequest(new { mesaj = "Gecersiz veya suresi dolmus 2FA kodu." });
        }

        var aliciHesapNo = istek.AliciHesapNo.Trim();
        var aliciHesap = await _context.Hesaplar.FirstOrDefaultAsync(h => h.HesapNo == aliciHesapNo);

        if (aliciHesap is null)
        {
            return BadRequest(new { mesaj = "Alici hesap bulunamadi." });
        }

        if (gonderenHesap.Id == aliciHesap.Id)
        {
            return BadRequest(new { mesaj = "Gonderen ve alici hesabi ayni olamaz." });
        }

        try
        {
            await _transferServis.TransferYapAsync(
                gonderenHesap.Id,
                aliciHesap.Id,
                istek.Miktar,
                "Para Transferi");

            return Ok(new
            {
                mesaj = "Transfer başarıyla tamamlandı.",
                aliciHesapId = aliciHesap.Id,
                aliciAdSoyad = aliciHesap.HesapSahibiAd
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mesaj = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { mesaj = "Beklenmeyen bir hata olustu." });
        }
    }
}
