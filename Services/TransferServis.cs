using System.Globalization;
using BankCoreApi.Data;
using BankCoreApi.Entities;
using BankCoreApi.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BankCoreApi.Services;

public class TransferServis : ITransferServis
{
    private const decimal KomisyonToplam = 2.50m;
    private const string HavuzEmail = "havuz@bankacuzdan.com";
    private const string UcretAciklama = "Transfer Ücreti ve BSMV";
    private const string KomisyonGelirAciklama = "Transfer Komisyon Geliri";

    private readonly BankaDbContext _context;
    private readonly IHesapServis _hesapServis;
    private readonly IHubContext<NotificationHub> _hubContext;

    public TransferServis(
        BankaDbContext context,
        IHesapServis hesapServis,
        IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hesapServis = hesapServis;
        _hubContext = hubContext;
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

            if (bakiye < Amount + KomisyonToplam)
            {
                throw new InvalidOperationException(
                    "Yetersiz bakiye. Transfer tutarı ve 2,50 TL işlem ücreti için bakiyeniz yetersiz.");
            }

            var havuzHesap = await _context.Hesaplar
                .FirstOrDefaultAsync(h => h.Email == HavuzEmail);

            if (havuzHesap is null)
            {
                var randomDigits = string.Join("", Enumerable.Range(0, 18).Select(_ => Random.Shared.Next(0, 10).ToString()));

                havuzHesap = new Hesap
                {
                    Id = Guid.NewGuid(),
                    HesapNo = "TR" + randomDigits,
                    HesapSahibiAd = "Banka Sistem Havuzu",
                    Email = HavuzEmail,
                    SifreHash = "system-no-login",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Hesaplar.Add(havuzHesap);
                await _context.SaveChangesAsync();
            }

            Guid islemGrupId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var paraCikisi = new DefterKayit
            {
                Id = Guid.NewGuid(),
                HesapId = gonderenHesapId,
                IslemGrupId = islemGrupId,
                Amount = -Amount,
                Aciklama = aciklama,
                CreatedAt = now
            };

            var paraGirisi = new DefterKayit
            {
                Id = Guid.NewGuid(),
                HesapId = aliciHesapId,
                IslemGrupId = islemGrupId,
                Amount = Amount,
                Aciklama = aciklama,
                CreatedAt = now
            };

            var ucretCikisi = new DefterKayit
            {
                Id = Guid.NewGuid(),
                HesapId = gonderenHesapId,
                IslemGrupId = islemGrupId,
                Amount = -KomisyonToplam,
                Aciklama = UcretAciklama,
                CreatedAt = now
            };

            var komisyonGirisi = new DefterKayit
            {
                Id = Guid.NewGuid(),
                HesapId = havuzHesap.Id,
                IslemGrupId = islemGrupId,
                Amount = KomisyonToplam,
                Aciklama = KomisyonGelirAciklama,
                CreatedAt = now
            };

            _context.DefterKayitlar.Add(paraCikisi);
            _context.DefterKayitlar.Add(paraGirisi);
            _context.DefterKayitlar.Add(ucretCikisi);
            _context.DefterKayitlar.Add(komisyonGirisi);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var miktarMetni = Amount.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"));
            await _hubContext.Clients.User(aliciHesapId.ToString())
                .SendAsync(
                    "ReceiveNotification",
                    $"Hesabınıza {miktarMetni} TL tutarında bir transfer geldi!");

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
