namespace BankCoreApi.Entities;

public class Hesap
{
    public Guid Id { get; set; }
    public string HesapNo { get; set; } = string.Empty;
    public string HesapSahibiAd { get; set; } = string.Empty;
    public string? TotpSecretKey { get; set; }
    public DateTime CreatedAt { get; set; }
}
