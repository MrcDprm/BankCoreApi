namespace BankCoreApi.Controllers.Dtos;

public record SanalKartUretIstek(string KartAdi, decimal AylikLimit);

public record SanalKartListeOge(
    Guid Id,
    string KartAdi,
    decimal AylikLimit,
    string KartNo,
    string Cvv,
    int SonKullanmaAy,
    int SonKullanmaYil,
    string Durum);
