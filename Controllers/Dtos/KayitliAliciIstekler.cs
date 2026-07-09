namespace BankCoreApi.Controllers.Dtos;

public record KayitliAliciKaydetIstek(Guid KarsiHesapId, string KayitliAd);

public record KayitliAliciListeOge(
    Guid Id,
    Guid KarsiHesapId,
    string Iban,
    string KayitliAd,
    DateTime CreatedAt);
