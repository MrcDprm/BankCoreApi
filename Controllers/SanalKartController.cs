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
    private const string SanalKartHavuzEmail = "sanalkart_havuz@bankacuzdan.com";
    private const string AktarimAciklama = "Sanal Karta Aktarım";
    private const string IadeAciklama = "Sanal Karttan İade";

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
            Bakiye = 0m,
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
            k.Bakiye,
            KriptoHelper.Coz(k.KartNoSifreli),
            KriptoHelper.Coz(k.CvvSifreli),
            k.SonKullanmaAy,
            k.SonKullanmaYil,
            k.Durum)).ToList();

        return Ok(liste);
    }

    [HttpPost("{kartId:guid}/para-yukle")]
    public async Task<IActionResult> ParaYukle([FromRoute] Guid kartId, [FromBody] SanalKartMiktarIstek istek)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userId, out var hesapId))
        {
            return Unauthorized();
        }

        if (istek.Miktar <= 0)
        {
            return BadRequest(new { mesaj = "Yüklenecek miktar 0'dan büyük olmalıdır." });
        }

        var kart = await _context.SanalKartlar
            .FirstOrDefaultAsync(k => k.Id == kartId && k.HesapId == hesapId && k.Durum == "Aktif");

        if (kart is null)
        {
            return NotFound(new { mesaj = "Sanal kart bulunamadı." });
        }

        var anaBakiye = await _context.DefterKayitlar
            .Where(d => d.HesapId == hesapId)
            .SumAsync(d => d.Amount);

        if (anaBakiye < istek.Miktar)
        {
            return BadRequest(new { mesaj = "Ana hesap bakiyeniz yetersiz." });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var havuzHesap = await SanalKartHavuzGetirVeyaOlusturAsync();
            var islemGrupId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            _context.DefterKayitlar.Add(new DefterKayit
            {
                Id = Guid.NewGuid(),
                HesapId = hesapId,
                IslemGrupId = islemGrupId,
                Amount = -istek.Miktar,
                Aciklama = AktarimAciklama,
                CreatedAt = now
            });

            _context.DefterKayitlar.Add(new DefterKayit
            {
                Id = Guid.NewGuid(),
                HesapId = havuzHesap.Id,
                IslemGrupId = islemGrupId,
                Amount = istek.Miktar,
                Aciklama = AktarimAciklama,
                CreatedAt = now
            });

            kart.Bakiye += istek.Miktar;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                mesaj = "Sanal karta para yüklendi.",
                kartBakiye = kart.Bakiye
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpPost("{kartId:guid}/para-cek")]
    public async Task<IActionResult> ParaCek([FromRoute] Guid kartId, [FromBody] SanalKartMiktarIstek istek)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userId, out var hesapId))
        {
            return Unauthorized();
        }

        if (istek.Miktar <= 0)
        {
            return BadRequest(new { mesaj = "Çekilecek miktar 0'dan büyük olmalıdır." });
        }

        var kart = await _context.SanalKartlar
            .FirstOrDefaultAsync(k => k.Id == kartId && k.HesapId == hesapId && k.Durum == "Aktif");

        if (kart is null)
        {
            return NotFound(new { mesaj = "Sanal kart bulunamadı." });
        }

        if (kart.Bakiye < istek.Miktar)
        {
            return BadRequest(new { mesaj = "Sanal kart bakiyesi yetersiz." });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var havuzHesap = await SanalKartHavuzGetirVeyaOlusturAsync();
            var islemGrupId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            _context.DefterKayitlar.Add(new DefterKayit
            {
                Id = Guid.NewGuid(),
                HesapId = havuzHesap.Id,
                IslemGrupId = islemGrupId,
                Amount = -istek.Miktar,
                Aciklama = IadeAciklama,
                CreatedAt = now
            });

            _context.DefterKayitlar.Add(new DefterKayit
            {
                Id = Guid.NewGuid(),
                HesapId = hesapId,
                IslemGrupId = islemGrupId,
                Amount = istek.Miktar,
                Aciklama = IadeAciklama,
                CreatedAt = now
            });

            kart.Bakiye -= istek.Miktar;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                mesaj = "Sanal karttan para çekildi.",
                kartBakiye = kart.Bakiye
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<Hesap> SanalKartHavuzGetirVeyaOlusturAsync()
    {
        var havuzHesap = await _context.Hesaplar
            .FirstOrDefaultAsync(h => h.Email == SanalKartHavuzEmail);

        if (havuzHesap is not null)
        {
            return havuzHesap;
        }

        var randomDigits = string.Join("", Enumerable.Range(0, 18).Select(_ => Random.Shared.Next(0, 10).ToString()));

        havuzHesap = new Hesap
        {
            Id = Guid.NewGuid(),
            HesapNo = "TR" + randomDigits,
            HesapSahibiAd = "Sanal Kart Havuzu",
            Email = SanalKartHavuzEmail,
            SifreHash = "system-no-login",
            CreatedAt = DateTime.UtcNow
        };

        _context.Hesaplar.Add(havuzHesap);
        await _context.SaveChangesAsync();
        return havuzHesap;
    }
}
