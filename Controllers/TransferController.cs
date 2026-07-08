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
        if (istek.Amount <= 0)
        {
            return BadRequest("Transfer miktari 0'dan buyuk olmalidir.");
        }

        if (istek.GonderenHesapId == istek.AliciHesapId)
        {
            return BadRequest("Gonderen ve alici hesabi ayni olamaz.");
        }

        var hesap = await _context.Hesaplar.FirstOrDefaultAsync(h => h.Id == istek.GonderenHesapId);
        if (hesap is null)
        {
            return BadRequest("Gonderen hesap bulunamadi.");
        }

        if (string.IsNullOrWhiteSpace(hesap.TotpSecretKey))
        {
            return BadRequest("Bu hesap icin 2FA aktif degil. Once TOTP kurun.");
        }

        if (!_totpServis.KoduDogrula(hesap.TotpSecretKey, istek.TotpCode))
        {
            return BadRequest("Gecersiz veya suresi dolmus 2FA kodu.");
        }

        try
        {
            await _transferServis.TransferYapAsync(
                istek.GonderenHesapId,
                istek.AliciHesapId,
                istek.Amount,
                istek.Aciklama ?? string.Empty);

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
