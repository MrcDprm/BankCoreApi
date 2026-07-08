using System.Security.Claims;
using BankCoreApi.Controllers.Dtos;
using BankCoreApi.Data;
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
    private readonly BankaDbContext _context;
    private readonly ITotpServis _totpServis;
    private readonly ITransferServis _transferServis;

    public TransferController(ITransferServis transferServis, ITotpServis totpServis, BankaDbContext context)
    {
        _transferServis = transferServis;
        _totpServis = totpServis;
        _context = context;
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

            return Ok(new { mesaj = "Transfer basariyla tamamlandi." });
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
