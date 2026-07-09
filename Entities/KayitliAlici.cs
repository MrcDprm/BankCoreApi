namespace BankCoreApi.Entities;

public class KayitliAlici
{
    public Guid Id { get; set; }
    public Guid HesapId { get; set; }
    public Guid KarsiHesapId { get; set; }
    public string Iban { get; set; } = string.Empty;
    public string KayitliAd { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
