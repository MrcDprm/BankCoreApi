using System.Security.Claims;
using BankCoreApi.Controllers.Dtos;
using BankCoreApi.Data;
using BankCoreApi.Entities;
using BankCoreApi.Helpers;
using BankCoreApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankCoreApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class HesapController : ControllerBase
{
    private const string TransferAciklama = "Para Transferi";
    private const string HavuzEmail = "havuz@bankacuzdan.com";

    private readonly BankaDbContext _context;
    private readonly IHesapServis _hesapServis;
    private readonly ITotpServis _totpServis;

    public HesapController(IHesapServis hesapServis, ITotpServis totpServis, BankaDbContext context)
    {
        _hesapServis = hesapServis;
        _totpServis = totpServis;
        _context = context;
    }

    [HttpGet("ozet")]
    public async Task<IActionResult> GetHesapOzet([FromQuery] int? ay)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userId, out var hesapId))
        {
            return Unauthorized();
        }

        if (ay is not null && ay is not (1 or 3 or 6 or 12 or 24))
        {
            return BadRequest(new { mesaj = "Geçersiz ay filtresi. İzin verilen değerler: 1, 3, 6, 12, 24." });
        }

        var ozet = await HesapOzetOlusturAsync(hesapId, ay);

        if (ozet is null)
        {
            return NotFound();
        }

        return Ok(ozet);
    }

    [HttpGet("ekstre/pdf")]
    public async Task<IActionResult> EkstrePdf([FromQuery] int? ay)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userId, out var hesapId))
        {
            return Unauthorized();
        }

        if (ay is not null && ay is not (1 or 3 or 6 or 12 or 24))
        {
            return BadRequest(new { mesaj = "Geçersiz ay filtresi. İzin verilen değerler: 1, 3, 6, 12, 24." });
        }

        var ozet = await HesapOzetOlusturAsync(hesapId, ay);

        if (ozet is null)
        {
            return NotFound();
        }

        var donem = ay is null ? "Son 5 İşlem" : $"Son {ay} Ay";
        var pdfBytes = PdfHelper.EkstreOlustur(ozet, donem);

        return File(pdfBytes, "application/pdf", "Ekstre.pdf");
    }

    private async Task<HesapOzetResponse?> HesapOzetOlusturAsync(Guid hesapId, int? ay)
    {
        var hesap = await _context.Hesaplar.FirstOrDefaultAsync(h => h.Id == hesapId);

        if (hesap is null)
        {
            return null;
        }

        var bakiye = await _context.DefterKayitlar
            .Where(d => d.HesapId == hesapId)
            .SumAsync(d => d.Amount);

        var kayitSorgu = _context.DefterKayitlar
            .Where(d => d.HesapId == hesapId);

        if (ay is null)
        {
            kayitSorgu = kayitSorgu
                .OrderByDescending(d => d.CreatedAt)
                .Take(5);
        }
        else
        {
            var baslangic = DateTime.UtcNow.AddMonths(-ay.Value);
            kayitSorgu = kayitSorgu
                .Where(d => d.CreatedAt >= baslangic)
                .OrderByDescending(d => d.CreatedAt)
                .Take(100);
        }

        var kayitlar = await kayitSorgu.ToListAsync();

        var transferGrupIdleri = kayitlar
            .Where(k => k.Aciklama == TransferAciklama)
            .Select(k => k.IslemGrupId)
            .Distinct()
            .ToList();

        var karsiHesapIdByGrup = new Dictionary<Guid, Guid>();

        if (transferGrupIdleri.Count > 0)
        {
            var karsiKayitlar = await _context.DefterKayitlar
                .Where(d =>
                    transferGrupIdleri.Contains(d.IslemGrupId) &&
                    d.HesapId != hesapId &&
                    d.Aciklama == TransferAciklama)
                .ToListAsync();

            foreach (var karsiKayit in karsiKayitlar)
            {
                karsiHesapIdByGrup.TryAdd(karsiKayit.IslemGrupId, karsiKayit.HesapId);
            }
        }

        var karsiHesapIdleri = karsiHesapIdByGrup.Values.Distinct().ToList();
        var karsiHesaplar = karsiHesapIdleri.Count > 0
            ? await _context.Hesaplar
                .Where(h => karsiHesapIdleri.Contains(h.Id))
                .ToDictionaryAsync(h => h.Id)
            : new Dictionary<Guid, Hesap>();

        var kayitliAlicilar = await _context.KayitliAlicilar
            .Where(k => k.HesapId == hesapId)
            .ToDictionaryAsync(k => k.KarsiHesapId, k => k.KayitliAd);

        var sonIslemler = kayitlar.Select(kayit =>
        {
            string? karsiHesapAdSoyad = null;

            if (kayit.Aciklama == TransferAciklama &&
                karsiHesapIdByGrup.TryGetValue(kayit.IslemGrupId, out var karsiHesapId) &&
                karsiHesaplar.TryGetValue(karsiHesapId, out var karsiHesap) &&
                !string.Equals(karsiHesap.Email, HavuzEmail, StringComparison.OrdinalIgnoreCase))
            {
                if (kayitliAlicilar.TryGetValue(karsiHesapId, out var kayitliAd) &&
                    !string.IsNullOrWhiteSpace(kayitliAd))
                {
                    karsiHesapAdSoyad = kayitliAd;
                }
                else
                {
                    karsiHesapAdSoyad = MaskelemeHelper.MaskeleAdSoyad(karsiHesap.HesapSahibiAd);
                    if (string.IsNullOrEmpty(karsiHesapAdSoyad))
                    {
                        karsiHesapAdSoyad = null;
                    }
                }
            }

            return new IslemOzet(
                kayit.Amount,
                kayit.Aciklama,
                kayit.CreatedAt,
                karsiHesapAdSoyad,
                kayit.IslemGrupId);
        }).ToList();

        var totpAktifMi = !string.IsNullOrWhiteSpace(hesap.TotpSecretKey);

        return new HesapOzetResponse(
            hesap.HesapSahibiAd,
            hesap.HesapNo,
            bakiye,
            totpAktifMi,
            sonIslemler);
    }

    [HttpGet("{id:guid}/bakiye")]
    public async Task<IActionResult> BakiyeGetir([FromRoute] Guid id)
    {
        var bakiye = await _hesapServis.BakiyeGetirAsync(id);
        return Ok(new { bakiye = bakiye });
    }

    [HttpPost("bakiye-yukle")]
    public async Task<IActionResult> BakiyeYukle([FromBody] BakiyeYukleIstek istek)
    {
        if (istek.Miktar <= 0)
        {
            return BadRequest("Yuklenecek miktar 0'dan buyuk olmalidir.");
        }

        if (istek.Miktar > 50000)
        {
            return BadRequest("Tek seferde en fazla 50.000 TL yüklenebilir");
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userId, out var hesapId))
        {
            return Unauthorized();
        }

        var hesap = await _context.Hesaplar.FirstOrDefaultAsync(h => h.Id == hesapId);

        if (hesap is null)
        {
            return NotFound();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var havuzHesap = await _context.Hesaplar
                .FirstOrDefaultAsync(h => h.Email == "havuz@bankacuzdan.com");

            if (havuzHesap is null)
            {
                var randomDigits = string.Join("", Enumerable.Range(0, 18).Select(_ => Random.Shared.Next(0, 10).ToString()));

                havuzHesap = new Hesap
                {
                    Id = Guid.NewGuid(),
                    HesapNo = "TR" + randomDigits,
                    HesapSahibiAd = "Banka Sistem Havuzu",
                    Email = "havuz@bankacuzdan.com",
                    SifreHash = "system-no-login",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Hesaplar.Add(havuzHesap);
                await _context.SaveChangesAsync();
            }

            Guid islemGrupId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            const string aciklama = "Kredi Kartı ile Bakiye Yükleme";

            var kullaniciKaydi = new DefterKayit
            {
                Id = Guid.NewGuid(),
                HesapId = hesapId,
                IslemGrupId = islemGrupId,
                Amount = istek.Miktar,
                Aciklama = aciklama,
                CreatedAt = now
            };

            var havuzKaydi = new DefterKayit
            {
                Id = Guid.NewGuid(),
                HesapId = havuzHesap.Id,
                IslemGrupId = islemGrupId,
                Amount = -istek.Miktar,
                Aciklama = aciklama,
                CreatedAt = now
            };

            _context.DefterKayitlar.Add(kullaniciKaydi);
            _context.DefterKayitlar.Add(havuzKaydi);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                mesaj = "Bakiye başarıyla yüklendi",
                yuklenenTutar = istek.Miktar
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpPut("profil-guncelle")]
    public async Task<IActionResult> ProfilGuncelle([FromBody] ProfilGuncelleIstek istek)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userId, out var hesapId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(istek.AdSoyad))
        {
            return BadRequest(new { mesaj = "Ad soyad boş olamaz." });
        }

        var hesap = await _context.Hesaplar.FirstOrDefaultAsync(h => h.Id == hesapId);

        if (hesap is null)
        {
            return NotFound();
        }

        hesap.HesapSahibiAd = istek.AdSoyad.Trim();
        await _context.SaveChangesAsync();

        return Ok(new { mesaj = "Profil güncellendi." });
    }

    [HttpPut("sifre-guncelle")]
    public async Task<IActionResult> SifreGuncelle([FromBody] SifreGuncelleIstek istek)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userId, out var hesapId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(istek.EskiSifre) || string.IsNullOrWhiteSpace(istek.YeniSifre))
        {
            return BadRequest(new { mesaj = "Mevcut şifre ve yeni şifre zorunludur." });
        }

        var hesap = await _context.Hesaplar.FirstOrDefaultAsync(h => h.Id == hesapId);

        if (hesap is null)
        {
            return NotFound();
        }

        if (!BCrypt.Net.BCrypt.Verify(istek.EskiSifre, hesap.SifreHash))
        {
            return BadRequest(new { mesaj = "Mevcut şifre hatalı." });
        }

        hesap.SifreHash = BCrypt.Net.BCrypt.HashPassword(istek.YeniSifre);
        await _context.SaveChangesAsync();

        return Ok(new { mesaj = "Şifre güncellendi." });
    }

    [HttpPost("{id:guid}/totp-kur")]
    public async Task<IActionResult> TotpKur([FromRoute] Guid id)
    {
        var hesap = await _context.Hesaplar.FirstOrDefaultAsync(h => h.Id == id);

        if (hesap is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(hesap.TotpSecretKey))
        {
            return BadRequest("TOTP zaten kurulu.");
        }

        var uretilenAnahtar = _totpServis.GizliAnahtarUret();
        hesap.TotpSecretKey = uretilenAnahtar;

        await _context.SaveChangesAsync();

        return Ok(new { secretKey = uretilenAnahtar });
    }
}
