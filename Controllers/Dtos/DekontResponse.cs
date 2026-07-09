namespace BankCoreApi.Controllers.Dtos;

public record DekontResponse(
    Guid IslemGrupId,
    string GonderenAd,
    string GonderenHesapNo,
    string AliciAd,
    string AliciHesapNo,
    decimal Tutar,
    decimal BsmvKesintisi,
    DateTime Tarih);
