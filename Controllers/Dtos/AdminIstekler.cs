namespace BankCoreApi.Controllers.Dtos;

public record FonEkleIstek(decimal Miktar);

public record BekleyenKrediOzet(
    Guid Id,
    string HesapSahibiAd,
    string KrediTuru,
    decimal AylikTaksit,
    decimal ToplamBorc,
    DateTime VadeTarihi,
    DateTime CreatedAt);
