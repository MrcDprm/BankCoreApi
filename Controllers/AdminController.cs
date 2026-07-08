using BankCoreApi.Controllers.Dtos;
using BankCoreApi.Data;
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
}

