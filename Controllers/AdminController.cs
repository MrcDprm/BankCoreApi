using BankCoreApi.Controllers.Dtos;
using BankCoreApi.Data;
using BankCoreApi.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankCoreApi.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly BankaDbContext _context;

    public AdminController(BankaDbContext context)
    {
        _context = context;
    }

    [HttpGet("banka-kasasi-ozet")]
    public async Task<ActionResult<BankaOzetResponse>> GetBankaKasasiOzet()
    {
        var defterToplam = await _context.DefterKayitlar
            .SumAsync(k => (decimal?)k.Amount) ?? 0m;

        var havuzHesap = await _context.Hesaplar
            .FirstOrDefaultAsync(h => h.Email == "havuz@bankacuzdan.com");

        decimal havuzBakiyesi = 0m;
        decimal kullanicilarToplamBakiyesi;

        if (havuzHesap is not null)
        {
            havuzBakiyesi = await _context.DefterKayitlar
                .Where(k => k.HesapId == havuzHesap.Id)
                .SumAsync(k => (decimal?)k.Amount) ?? 0m;

            kullanicilarToplamBakiyesi = await _context.DefterKayitlar
                .Where(k => k.HesapId != havuzHesap.Id)
                .SumAsync(k => (decimal?)k.Amount) ?? 0m;
        }
        else
        {
            kullanicilarToplamBakiyesi = await _context.DefterKayitlar
                .SumAsync(k => (decimal?)k.Amount) ?? 0m;
        }

        return Ok(new BankaOzetResponse
        {
            DefterToplam = defterToplam,
            HavuzBakiyesi = havuzBakiyesi,
            KullanicilarToplamBakiyesi = kullanicilarToplamBakiyesi
        });
    }

    [HttpGet("bekleyen-krediler")]
    public async Task<ActionResult<List<BekleyenKrediOzet>>> BekleyenKrediler()
    {
        var liste = await (
            from k in _context.Krediler
            join h in _context.Hesaplar on k.HesapId equals h.Id
            where k.Durum == "Beklemede"
            orderby k.CreatedAt ascending
            select new BekleyenKrediOzet(
                k.Id,
                h.HesapSahibiAd,
                k.KrediTuru,
                k.AylikTaksit,
                k.ToplamBorc,
                k.VadeTarihi,
                k.CreatedAt)
        ).ToListAsync();

        return Ok(liste);
    }

    [HttpPost("kredi-onayla/{krediId:guid}")]
    public async Task<IActionResult> KrediOnayla([FromRoute] Guid krediId)
    {
        var kredi = await _context.Krediler.FirstOrDefaultAsync(k => k.Id == krediId);

        if (kredi is null)
        {
            return NotFound(new { mesaj = "Kredi bulunamadı." });
        }

        if (kredi.Durum != "Beklemede")
        {
            return BadRequest(new { mesaj = "Yalnızca bekleyen krediler onaylanabilir." });
        }

        var havuzHesap = await _context.Hesaplar
            .FirstOrDefaultAsync(h => h.Email == "havuz@bankacuzdan.com");

        if (havuzHesap is null)
        {
            return BadRequest(new { mesaj = "Banka sistem havuzu hesabı bulunamadı." });
        }

        var havuzBakiyesi = await _context.DefterKayitlar
            .Where(k => k.HesapId == havuzHesap.Id)
            .SumAsync(k => (decimal?)k.Amount) ?? 0m;

        if (havuzBakiyesi < kredi.AnaPara)
        {
            return BadRequest(new { mesaj = "Banka kasasında bu krediyi onaylamak için yeterli likidite bulunmuyor." });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var now = DateTime.UtcNow;
            var islemGrupId = Guid.NewGuid();
            const string aciklama = "Kredi Tahsisi";

            var havuzKaydi = new DefterKayit
            {
                Id = Guid.NewGuid(),
                HesapId = havuzHesap.Id,
                IslemGrupId = islemGrupId,
                Amount = -kredi.AnaPara,
                Aciklama = aciklama,
                CreatedAt = now
            };

            var kullaniciKaydi = new DefterKayit
            {
                Id = Guid.NewGuid(),
                HesapId = kredi.HesapId,
                IslemGrupId = islemGrupId,
                Amount = kredi.AnaPara,
                Aciklama = aciklama,
                CreatedAt = now
            };

            kredi.Durum = "Aktif";
            kredi.SonrakiTaksitTarihi = now.AddMonths(1);

            _context.DefterKayitlar.Add(havuzKaydi);
            _context.DefterKayitlar.Add(kullaniciKaydi);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { mesaj = "Kredi onaylandı ve hesaba aktarıldı." });
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { mesaj = "Beklenmeyen bir hata oluştu." });
        }
    }

    [HttpPost("kredi-reddet/{krediId:guid}")]
    public async Task<IActionResult> KrediReddet([FromRoute] Guid krediId)
    {
        var kredi = await _context.Krediler.FirstOrDefaultAsync(k => k.Id == krediId);

        if (kredi is null)
        {
            return NotFound(new { mesaj = "Kredi bulunamadı." });
        }

        if (kredi.Durum != "Beklemede")
        {
            return BadRequest(new { mesaj = "Yalnızca bekleyen krediler reddedilebilir." });
        }

        kredi.Durum = "Reddedildi";
        await _context.SaveChangesAsync();

        return Ok(new { mesaj = "Kredi başvurusu reddedildi." });
    }

    [HttpPost("fon-ekle")]
    public async Task<IActionResult> FonEkle([FromBody] FonEkleIstek istek)
    {
        if (istek.Miktar <= 0)
        {
            return BadRequest("Enjekte edilecek miktar 0'dan büyük olmalıdır.");
        }

        if (istek.Miktar > 1_000_000_000m)
        {
            return BadRequest("Tek seferde en fazla 1 Milyar TL fon enjekte edilebilir.");
        }

        var havuzHesap = await _context.Hesaplar
            .FirstOrDefaultAsync(h => h.Email == "havuz@bankacuzdan.com");

        if (havuzHesap is null)
        {
            return BadRequest("Banka sistem havuzu hesabı bulunamadı.");
        }

        var kayit = new DefterKayit
        {
            Id = Guid.NewGuid(),
            HesapId = havuzHesap.Id,
            IslemGrupId = Guid.NewGuid(),
            Amount = istek.Miktar,
            Aciklama = "Merkez Bankası Fon Enjeksiyonu",
            CreatedAt = DateTime.UtcNow
        };

        _context.DefterKayitlar.Add(kayit);
        await _context.SaveChangesAsync();

        return Ok(new { mesaj = "Fon başarıyla kasaya enjekte edildi." });
    }
}
