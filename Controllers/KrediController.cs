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

        const decimal faizOrani = 0.20m;
        var faizTutari = istek.Miktar * faizOrani;
        var toplamBorc = istek.Miktar + faizTutari;

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
                FaizOrani = faizOrani,
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
                toplamBorc = toplamBorc
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { mesaj = "Beklenmeyen bir hata oluştu." });
        }
    }
}

