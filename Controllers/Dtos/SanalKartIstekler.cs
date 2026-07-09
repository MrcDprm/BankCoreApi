namespace BankCoreApi.Controllers.Dtos;

public record SanalKartUretIstek(string KartAdi, decimal AylikLimit);

public record SanalKartMiktarIstek(decimal Miktar);

public record SanalKartListeOge(
    Guid Id,
    string KartAdi,
    decimal AylikLimit,
    decimal Bakiye,
    string KartNo,
    string Cvv,
    int SonKullanmaAy,
    int SonKullanmaYil,
    string Durum);
