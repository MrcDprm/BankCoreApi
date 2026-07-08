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

