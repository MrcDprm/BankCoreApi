using BankCoreApi.Entities;

namespace BankCoreApi.Services;

public interface IHesapServis
{
    Task<decimal> BakiyeGetirAsync(Guid hesapId);
    Task<Hesap> HesapOlusturAsync(string hesapSahibiAd);
}
