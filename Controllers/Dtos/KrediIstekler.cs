namespace BankCoreApi.Controllers.Dtos;

public record KrediBasvuruIstek(decimal Miktar, int VadeAy, string KrediTuru);

public record KrediOzetResponse(
    Guid Id,
    string KrediTuru,
    decimal ToplamBorc,
    decimal KalanBorc,
    decimal AylikTaksit,
    DateTime VadeTarihi,
    string Durum);
