using BankCoreApi.Data;
using BankCoreApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankCoreApi.Services;

public class TransferServis : ITransferServis
{
    private readonly BankaDbContext _context;
    private readonly IHesapServis _hesapServis;

    public TransferServis(BankaDbContext context, IHesapServis hesapServis)
    {
        _context = context;
        _hesapServis = hesapServis;
    }

    public async Task<bool> TransferYapAsync(Guid gonderenHesapId, Guid aliciHesapId, decimal Amount, string aciklama)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await _context.Hesaplar
                .FromSqlRaw("SELECT * FROM \"Hesaplar\" WHERE \"Id\" = {0} FOR UPDATE", gonderenHesapId)
                .FirstOrDefaultAsync();

            var bakiye = await _hesapServis.BakiyeGetirAsync(gonderenHesapId);

            if (bakiye < Amount)
            {
                throw new InvalidOperationException("Yetersiz bakiye.");
            }

            Guid islemGrupId = Guid.NewGuid();

            var paraCikisi = new DefterKayit
            {
                Id = Guid.NewGuid(),
                HesapId = gonderenHesapId,
                IslemGrupId = islemGrupId,
                Amount = -Amount,
                Aciklama = aciklama,
                CreatedAt = DateTime.UtcNow
            };

            var paraGirisi = new DefterKayit
            {
                Id = Guid.NewGuid(),
                HesapId = aliciHesapId,
                IslemGrupId = islemGrupId,
                Amount = Amount,
                Aciklama = aciklama,
                CreatedAt = DateTime.UtcNow
            };

            _context.DefterKayitlar.Add(paraCikisi);
            _context.DefterKayitlar.Add(paraGirisi);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
