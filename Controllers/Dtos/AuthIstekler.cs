namespace BankCoreApi.Controllers.Dtos;

public record KayitIstek(string HesapSahibiAd, string Email, string Sifre);

public record GirisIstek(string Email, string Sifre);
