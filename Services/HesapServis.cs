using BankCoreApi.Data;
using BankCoreApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankCoreApi.Services;

public class HesapServis : IHesapServis
{
    private readonly BankaDbContext _context;

    public HesapServis(BankaDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> BakiyeGetirAsync(Guid hesapId)
    {
        return await _context.DefterKayitlar
            .Where(d => d.HesapId == hesapId)
            .SumAsync(d => d.Amount);
    }

    public async Task<Hesap> HesapOlusturAsync(string hesapSahibiAd)
    {
        var randomDigits = string.Join("", Enumerable.Range(0, 18).Select(_ => Random.Shared.Next(0, 10).ToString()));

        var hesap = new Hesap
        {
            Id = Guid.NewGuid(),
            HesapNo = "TR" + randomDigits,
            HesapSahibiAd = hesapSahibiAd,
            CreatedAt = DateTime.UtcNow
        };

        _context.Hesaplar.Add(hesap);
        await _context.SaveChangesAsync();

        return hesap;
    }
}
