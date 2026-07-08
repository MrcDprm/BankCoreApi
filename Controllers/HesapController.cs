using System.Security.Claims;
using BankCoreApi.Controllers.Dtos;
using BankCoreApi.Data;
using BankCoreApi.Entities;
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
