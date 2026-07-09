using System.Security.Claims;
using BankCoreApi.Controllers.Dtos;
using BankCoreApi.Data;
using BankCoreApi.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankCoreApi.Controllers;

[Authorize]
[ApiController]
[Route("api/kayitli-alicilar")]
public class KayitliAlicilarController : ControllerBase
{
    private readonly BankaDbContext _context;

    public KayitliAlicilarController(BankaDbContext context)
    {
        _context = context;
    }

    [HttpPost("kaydet")]
    public async Task<IActionResult> Kaydet([FromBody] KayitliAliciKaydetIstek istek)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userId, out var hesapId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(istek.KayitliAd))
        {
            return BadRequest(new { mesaj = "Kayıtlı ad boş olamaz." });
        }

        if (istek.KarsiHesapId == Guid.Empty)
        {
            return BadRequest(new { mesaj = "Karşı hesap bilgisi zorunludur." });
        }

        if (istek.KarsiHesapId == hesapId)
        {
            return BadRequest(new { mesaj = "Kendi hesabınızı kayıtlı alıcı olarak ekleyemezsiniz." });
        }

        var karsiHesap = await _context.Hesaplar.FirstOrDefaultAsync(h => h.Id == istek.KarsiHesapId);

        if (karsiHesap is null)
        {
            return BadRequest(new { mesaj = "Karşı hesap bulunamadı." });
        }

        var mevcut = await _context.KayitliAlicilar
            .AnyAsync(k => k.HesapId == hesapId && k.KarsiHesapId == istek.KarsiHesapId);

        if (mevcut)
        {
            return BadRequest(new { mesaj = "Bu alıcı zaten rehberinizde kayıtlı." });
        }

        var kayit = new KayitliAlici
        {
            Id = Guid.NewGuid(),
            HesapId = hesapId,
            KarsiHesapId = istek.KarsiHesapId,
            Iban = karsiHesap.HesapNo,
            KayitliAd = istek.KayitliAd.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.KayitliAlicilar.Add(kayit);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mesaj = "Alıcı başarıyla kaydedildi.",
            id = kayit.Id
        });
    }

    [HttpGet("listele")]
    public async Task<IActionResult> Listele()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userId, out var hesapId))
        {
            return Unauthorized();
        }

        var liste = await _context.KayitliAlicilar
            .Where(k => k.HesapId == hesapId)
            .OrderBy(k => k.KayitliAd)
            .Select(k => new KayitliAliciListeOge(
                k.Id,
                k.KarsiHesapId,
                k.Iban,
                k.KayitliAd,
                k.CreatedAt))
            .ToListAsync();

        return Ok(liste);
    }
}
