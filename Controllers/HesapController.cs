using BankCoreApi.Controllers.Dtos;
using BankCoreApi.Data;
using BankCoreApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankCoreApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HesapController : ControllerBase
{
    private readonly BankaDbContext _context;
    private readonly IHesapServis _hesapServis;
    private readonly ITotpServis _totpServis;

    public HesapController(IHesapServis hesapServis, ITotpServis totpServis, BankaDbContext context)
    {
        _hesapServis = hesapServis;
        _totpServis = totpServis;
        _context = context;
    }

    [HttpPost("olustur")]
    public async Task<IActionResult> Olustur([FromBody] HesapOlusturIstek istek)
    {
        if (string.IsNullOrWhiteSpace(istek.HesapSahibiAd))
        {
            return BadRequest("Hesap sahibi adi bos olamaz.");
        }

        var hesap = await _hesapServis.HesapOlusturAsync(istek.HesapSahibiAd);
        return Ok(hesap);
    }

    [HttpGet("{id:guid}/bakiye")]
    public async Task<IActionResult> BakiyeGetir([FromRoute] Guid id)
    {
        var bakiye = await _hesapServis.BakiyeGetirAsync(id);
        return Ok(new { bakiye = bakiye });
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
