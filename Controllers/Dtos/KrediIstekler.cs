namespace BankCoreApi.Controllers.Dtos;

public record KrediBasvuruIstek(decimal Miktar, int VadeAy, string KrediTuru, string KrediAltTuru);

public record KrediOzetResponse(
    Guid Id,
    string KrediTuru,
    string KrediAltTuru,
    decimal ToplamBorc,
    decimal KalanBorc,
    decimal AylikTaksit,
    DateTime VadeTarihi,
    string Durum,
    DateTime? SonrakiTaksitTarihi);
