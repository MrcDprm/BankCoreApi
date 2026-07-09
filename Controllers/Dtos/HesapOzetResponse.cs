namespace BankCoreApi.Controllers.Dtos;

public record IslemOzet(decimal Miktar, string Aciklama, DateTime Tarih, string? KarsiHesapAdSoyad);

public record HesapOzetResponse(
    string HesapSahibiAd,
    string HesapNo,
    decimal Bakiye,
    bool TotpAktifMi,
    List<IslemOzet> SonIslemler);
