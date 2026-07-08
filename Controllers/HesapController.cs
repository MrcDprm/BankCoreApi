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

    [HttpGet("ozet")]
    public async Task<IActionResult> GetHesapOzet()
    {
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

        var bakiye = await _context.DefterKayitlar
            .Where(d => d.HesapId == hesapId)
            .SumAsync(d => d.Amount);

        var sonIslemler = await _context.DefterKayitlar
            .Where(d => d.HesapId == hesapId)
            .OrderByDescending(d => d.CreatedAt)
            .Take(10)
            .Select(d => new IslemOzet(d.Amount, d.Aciklama, d.CreatedAt))
            .ToListAsync();

        var totpAktifMi = !string.IsNullOrWhiteSpace(hesap.TotpSecretKey);

        var ozet = new HesapOzetResponse(
            hesap.HesapSahibiAd,
            hesap.HesapNo,
            bakiye,
            totpAktifMi,
            sonIslemler);

        return Ok(ozet);
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
