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
[Route("api/[controller]")]
public class KrediController : ControllerBase
{
    private readonly BankaDbContext _context;

    public KrediController(BankaDbContext context)
    {
        _context = context;
    }

    [HttpPost("basvur")]
    public async Task<IActionResult> Basvur([FromBody] KrediBasvuruIstek istek)
    {
        if (istek.Miktar <= 0)
        {
            return BadRequest("Kredi miktarı 0'dan büyük olmalıdır.");
        }

        if (istek.VadeAy < 1 || istek.VadeAy > 36)
        {
            return BadRequest("Vade 1 ile 36 ay arasında olmalıdır.");
        }

        if (!TryGetAylikFaizOrani(istek.KrediTuru, out var aylikFaizOrani))
        {
            return BadRequest("Geçerli bir kredi türü seçin.");
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userId, out var hesapId))
        {
            return Unauthorized();
        }

        var havuzHesap = await _context.Hesaplar.FirstOrDefaultAsync(h => h.Email == "havuz@bankacuzdan.com");

        if (havuzHesap is null)
        {
            return BadRequest("Banka sistem havuzu hesabı bulunamadı.");
        }

        var havuzBakiyesi = await _context.DefterKayitlar
            .Where(k => k.HesapId == havuzHesap.Id)
            .SumAsync(k => (decimal?)k.Amount) ?? 0m;

        if (havuzBakiyesi < istek.Miktar)
        {
            return BadRequest("Banka kasasında bu işlemi gerçekleştirmek için yeterli likidite bulunmuyor.");
        }

        var aylikTaksit = HesaplaAylikTaksit(istek.Miktar, aylikFaizOrani, istek.VadeAy);
        var toplamBorc = aylikTaksit * istek.VadeAy;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var now = DateTime.UtcNow;
            var islemGrupId = Guid.NewGuid();
            const string aciklama = "Kredi Tahsisi";

            var kredi = new Kredi
            {
                Id = Guid.NewGuid(),
                HesapId = hesapId,
                AnaPara = istek.Miktar,
                FaizOrani = aylikFaizOrani,
                KrediTuru = istek.KrediTuru,
                AylikFaizOrani = aylikFaizOrani,
                AylikTaksit = aylikTaksit,
                ToplamBorc = toplamBorc,
                KalanBorc = toplamBorc,
                VadeTarihi = now.AddMonths(istek.VadeAy),
                Durum = "Aktif",
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

            var kullaniciKaydi = new DefterKayit
            {
                Id = Guid.NewGuid(),
                HesapId = hesapId,
                IslemGrupId = islemGrupId,
                Amount = istek.Miktar,
                Aciklama = aciklama,
                CreatedAt = now
            };

            _context.Krediler.Add(kredi);
            _context.DefterKayitlar.Add(havuzKaydi);
            _context.DefterKayitlar.Add(kullaniciKaydi);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                mesaj = "Kredi onaylandı ve hesabınıza aktarıldı.",
                aylikTaksit = aylikTaksit,
                toplamBorc = toplamBorc
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { mesaj = "Beklenmeyen bir hata oluştu." });
        }
    }

    [HttpGet("aktif-krediler")]
    public async Task<ActionResult<List<KrediOzetResponse>>> AktifKrediler()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userId, out var hesapId))
        {
            return Unauthorized();
        }

        var krediler = await _context.Krediler
            .Where(k => k.HesapId == hesapId && k.Durum != "Kapandi")
            .OrderByDescending(k => k.VadeTarihi)
            .Select(k => new KrediOzetResponse(
                k.Id,
                k.KrediTuru,
                k.ToplamBorc,
                k.KalanBorc,
                k.AylikTaksit,
                k.VadeTarihi,
                k.Durum))
            .ToListAsync();

        return Ok(krediler);
    }

    [HttpPost("taksit-ode/{krediId:guid}")]
    public async Task<IActionResult> TaksitOde([FromRoute] Guid krediId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userId, out var hesapId))
        {
            return Unauthorized();
        }

        var kredi = await _context.Krediler.FirstOrDefaultAsync(k => k.Id == krediId);

        if (kredi is null || kredi.HesapId != hesapId)
        {
            return BadRequest(new { mesaj = "Kredi bulunamadı." });
        }

        if (kredi.Durum == "Kapandi")
        {
            return BadRequest(new { mesaj = "Bu kredi için ödeme yapılamaz." });
        }

        var odenecekTutar = kredi.KalanBorc < kredi.AylikTaksit ? kredi.KalanBorc : kredi.AylikTaksit;

        if (odenecekTutar <= 0)
        {
            kredi.KalanBorc = 0;
            kredi.Durum = "Kapandi";
            await _context.SaveChangesAsync();
            return Ok(new { mesaj = "Kredi kapatıldı." });
        }

        var bakiye = await _context.DefterKayitlar
            .Where(d => d.HesapId == hesapId)
            .SumAsync(d => (decimal?)d.Amount) ?? 0m;

        if (bakiye < odenecekTutar)
        {
            return BadRequest("Vadesiz hesabınızda taksit ödemesi için yeterli bakiye bulunmuyor.");
        }

        var havuzHesap = await _context.Hesaplar.FirstOrDefaultAsync(h => h.Email == "havuz@bankacuzdan.com");

        if (havuzHesap is null)
        {
            return BadRequest(new { mesaj = "Banka sistem havuzu hesabı bulunamadı." });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var now = DateTime.UtcNow;
            var islemGrupId = Guid.NewGuid();
            var aciklama = $"Kredi Taksit Tahsilatı - {kredi.KrediTuru}";

            var kullaniciKaydi = new DefterKayit
            {
                Id = Guid.NewGuid(),
                HesapId = hesapId,
                IslemGrupId = islemGrupId,
                Amount = -odenecekTutar,
                Aciklama = aciklama,
                CreatedAt = now
            };

            var havuzKaydi = new DefterKayit
            {
                Id = Guid.NewGuid(),
                HesapId = havuzHesap.Id,
                IslemGrupId = islemGrupId,
                Amount = odenecekTutar,
                Aciklama = aciklama,
                CreatedAt = now
            };

            kredi.KalanBorc -= odenecekTutar;

            if (kredi.KalanBorc <= 0)
            {
                kredi.KalanBorc = 0;
                kredi.Durum = "Kapandi";
            }

            _context.DefterKayitlar.Add(kullaniciKaydi);
            _context.DefterKayitlar.Add(havuzKaydi);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { mesaj = "Taksit ödemesi başarıyla alındı." });
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { mesaj = "Beklenmeyen bir hata oluştu." });
        }
    }

    private static bool TryGetAylikFaizOrani(string krediTuru, out decimal aylikFaizOrani)
    {
        aylikFaizOrani = krediTuru switch
        {
            "Ihtiyac" => 0.035m,
            "Tasit" => 0.025m,
            "Konut" => 0.012m,
            _ => -1m
        };

        return aylikFaizOrani >= 0m;
    }

    private static decimal HesaplaAylikTaksit(decimal miktar, decimal aylikFaizOrani, int vadeAy)
    {
        if (aylikFaizOrani == 0m)
        {
            return miktar / vadeAy;
        }

        var pow = (decimal)Math.Pow(1 + (double)aylikFaizOrani, vadeAy);
        return miktar * (aylikFaizOrani * pow) / (pow - 1m);
    }
}
