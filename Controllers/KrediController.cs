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
    private const decimal SonTaksitTolerans = 2m;
    private const decimal GecikmeGunlukOran = 0.005m;

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
            return BadRequest(new { mesaj = "Kredi miktarı 0'dan büyük olmalıdır." });
        }

        if (istek.VadeAy < 1 || istek.VadeAy > 36)
        {
            return BadRequest(new { mesaj = "Vade 1 ile 36 ay arasında olmalıdır." });
        }

        if (string.IsNullOrWhiteSpace(istek.KrediAltTuru))
        {
            return BadRequest(new { mesaj = "Kredi alt türü seçilmelidir." });
        }

        if (!TryGetAylikFaizOrani(istek.KrediTuru, istek.KrediAltTuru, istek.VadeAy, out var aylikFaizOrani))
        {
            return BadRequest(new { mesaj = "Geçerli bir kredi türü ve alt türü seçin." });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userId, out var hesapId))
        {
            return Unauthorized();
        }

        var bekleyenVar = await _context.Krediler
            .AnyAsync(k => k.HesapId == hesapId && k.Durum == "Beklemede");

        if (bekleyenVar)
        {
            return BadRequest(new { mesaj = "Zaten bekleyen bir kredi başvurunuz var." });
        }

        var aylikTaksit = Math.Round(HesaplaAylikTaksit(istek.Miktar, aylikFaizOrani, istek.VadeAy), 2);
        var toplamBorc = Math.Round(aylikTaksit * istek.VadeAy, 2);
        var now = DateTime.UtcNow;

        var kredi = new Kredi
        {
            Id = Guid.NewGuid(),
            HesapId = hesapId,
            AnaPara = istek.Miktar,
            FaizOrani = aylikFaizOrani,
            KrediTuru = istek.KrediTuru,
            KrediAltTuru = istek.KrediAltTuru,
            AylikFaizOrani = aylikFaizOrani,
            AylikTaksit = aylikTaksit,
            ToplamBorc = toplamBorc,
            KalanBorc = toplamBorc,
            VadeTarihi = now.AddMonths(istek.VadeAy),
            SonrakiTaksitTarihi = null,
            Durum = "Beklemede",
            CreatedAt = now
        };

        _context.Krediler.Add(kredi);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mesaj = "Kredi başvurunuz alındı. Onay bekleniyor.",
            aylikTaksit = aylikTaksit,
            toplamBorc = toplamBorc,
            aylikFaizOrani = aylikFaizOrani
        });
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
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new KrediOzetResponse(
                k.Id,
                k.KrediTuru,
                k.KrediAltTuru,
                k.ToplamBorc,
                k.KalanBorc,
                k.AylikTaksit,
                k.VadeTarihi,
                k.Durum,
                k.SonrakiTaksitTarihi))
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

        if (kredi.Durum != "Aktif")
        {
            return BadRequest(new { mesaj = "Yalnızca aktif krediler için taksit ödemesi yapılabilir." });
        }

        if (!kredi.SonrakiTaksitTarihi.HasValue)
        {
            return BadRequest(new { mesaj = "Bu kredi için ödeme tarihi tanımlı değil." });
        }

        var now = DateTime.UtcNow;
        var gunFarki = (now.Date - kredi.SonrakiTaksitTarihi.Value.Date).Days;

        if (gunFarki < -5)
        {
            return BadRequest(new
            {
                mesaj = "Henüz ödeme döneminde değilsiniz. Taksitler ödeme gününe en fazla 5 gün kala ödenebilir."
            });
        }

        var borcDusumu = kredi.KalanBorc <= kredi.AylikTaksit + SonTaksitTolerans
            ? kredi.KalanBorc
            : kredi.AylikTaksit;

        if (borcDusumu <= 0)
        {
            kredi.KalanBorc = 0;
            kredi.Durum = "Kapandi";
            kredi.SonrakiTaksitTarihi = null;
            await _context.SaveChangesAsync();
            return Ok(new { mesaj = "Kredi kapatıldı." });
        }

        var gecikmeli = gunFarki > 0;
        var tahsilatTutari = gecikmeli
            ? Math.Round(borcDusumu * (1m + GecikmeGunlukOran * gunFarki), 2)
            : borcDusumu;

        var bakiye = await _context.DefterKayitlar
            .Where(d => d.HesapId == hesapId)
            .SumAsync(d => (decimal?)d.Amount) ?? 0m;

        if (bakiye < tahsilatTutari)
        {
            return BadRequest(new { mesaj = "Vadesiz hesabınızda taksit ödemesi için yeterli bakiye bulunmuyor." });
        }

        var havuzHesap = await _context.Hesaplar.FirstOrDefaultAsync(h => h.Email == "havuz@bankacuzdan.com");

        if (havuzHesap is null)
        {
            return BadRequest(new { mesaj = "Banka sistem havuzu hesabı bulunamadı." });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var islemGrupId = Guid.NewGuid();
            var aciklama = gecikmeli
                ? $"Kredi Taksit Tahsilatı (Gecikme Faizi) - {kredi.KrediTuru}"
                : $"Kredi Taksit Tahsilatı - {kredi.KrediTuru}";

            var kullaniciKaydi = new DefterKayit
            {
                Id = Guid.NewGuid(),
                HesapId = hesapId,
                IslemGrupId = islemGrupId,
                Amount = -tahsilatTutari,
                Aciklama = aciklama,
                CreatedAt = now
            };

            var havuzKaydi = new DefterKayit
            {
                Id = Guid.NewGuid(),
                HesapId = havuzHesap.Id,
                IslemGrupId = islemGrupId,
                Amount = tahsilatTutari,
                Aciklama = aciklama,
                CreatedAt = now
            };

            kredi.KalanBorc -= borcDusumu;

            if (kredi.KalanBorc <= 0)
            {
                kredi.KalanBorc = 0;
                kredi.Durum = "Kapandi";
                kredi.SonrakiTaksitTarihi = null;
            }
            else
            {
                kredi.SonrakiTaksitTarihi = kredi.SonrakiTaksitTarihi.Value.AddMonths(1);
            }

            _context.DefterKayitlar.Add(kullaniciKaydi);
            _context.DefterKayitlar.Add(havuzKaydi);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                mesaj = gecikmeli
                    ? "Gecikmeli taksit ödemesi (gecikme faizi dahil) alındı."
                    : "Taksit ödemesi başarıyla alındı.",
                tahsilEdilen = tahsilatTutari,
                borcDusumu = borcDusumu,
                gecikmeGunu = gecikmeli ? gunFarki : 0
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { mesaj = "Beklenmeyen bir hata oluştu." });
        }
    }

    private static bool TryGetAylikFaizOrani(
        string krediTuru,
        string krediAltTuru,
        int vadeAy,
        out decimal aylikFaizOrani)
    {
        decimal? baz = (krediTuru, krediAltTuru) switch
        {
            ("Ihtiyac", "Egitim") => 0.030m,
            ("Ihtiyac", "Tatil") => 0.035m,
            ("Ihtiyac", "BorcKapatma") => 0.040m,
            ("Tasit", "SifirKm") => 0.022m,
            ("Tasit", "IkinciEl") => 0.028m,
            ("Konut", "IlkEvim") => 0.012m,
            ("Konut", "Yatirim") => 0.018m,
            _ => null
        };

        if (baz is null)
        {
            aylikFaizOrani = -1m;
            return false;
        }

        var ekstraDonem = Math.Max(0, (vadeAy - 12) / 12);
        var riskPrimi = ekstraDonem * 0.001m;
        aylikFaizOrani = baz.Value + riskPrimi;
        return true;
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
