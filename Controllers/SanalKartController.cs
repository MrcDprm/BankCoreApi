using System.Security.Claims;
using BankCoreApi.Controllers.Dtos;
using BankCoreApi.Data;
using BankCoreApi.Entities;
using BankCoreApi.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankCoreApi.Controllers;

[Authorize]
[ApiController]
[Route("api/sanal-kart")]
public class SanalKartController : ControllerBase
{
    private const int MaxAktifKart = 3;

    private readonly BankaDbContext _context;

    public SanalKartController(BankaDbContext context)
    {
        _context = context;
    }

    [HttpPost("uret")]
    public async Task<IActionResult> Uret([FromBody] SanalKartUretIstek istek)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userId, out var hesapId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(istek.KartAdi))
        {
            return BadRequest(new { mesaj = "Kart adı boş olamaz." });
        }

        if (istek.AylikLimit <= 0)
        {
            return BadRequest(new { mesaj = "Aylık limit 0'dan büyük olmalıdır." });
        }

        var aktifKartSayisi = await _context.SanalKartlar
            .CountAsync(k => k.HesapId == hesapId && k.Durum == "Aktif");

        if (aktifKartSayisi >= MaxAktifKart)
        {
            return BadRequest(new { mesaj = "En fazla 3 sanal kart oluşturabilirsiniz." });
        }

        var uretilen = KartHelper.Uret();

        var kart = new SanalKart
        {
            Id = Guid.NewGuid(),
            HesapId = hesapId,
            KartAdi = istek.KartAdi.Trim(),
            AylikLimit = istek.AylikLimit,
            KartNoSifreli = KriptoHelper.Sifrele(uretilen.KartNo),
            CvvSifreli = KriptoHelper.Sifrele(uretilen.Cvv),
            SonKullanmaAy = uretilen.SonKullanmaAy,
            SonKullanmaYil = uretilen.SonKullanmaYil,
            Durum = "Aktif"
        };

        _context.SanalKartlar.Add(kart);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mesaj = "Sanal kart başarıyla oluşturuldu.",
            id = kart.Id
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

        var kartlar = await _context.SanalKartlar
            .Where(k => k.HesapId == hesapId && k.Durum == "Aktif")
            .OrderByDescending(k => k.SonKullanmaYil)
            .ThenByDescending(k => k.SonKullanmaAy)
            .ToListAsync();

        var liste = kartlar.Select(k => new SanalKartListeOge(
            k.Id,
            k.KartAdi,
            k.AylikLimit,
            KriptoHelper.Coz(k.KartNoSifreli),
            KriptoHelper.Coz(k.CvvSifreli),
            k.SonKullanmaAy,
            k.SonKullanmaYil,
            k.Durum)).ToList();

        return Ok(liste);
    }
}
