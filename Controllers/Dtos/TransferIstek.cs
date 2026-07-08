namespace BankCoreApi.Controllers.Dtos;

public class TransferIstek
{
    public Guid GonderenHesapId { get; set; }
    public Guid AliciHesapId { get; set; }
    public decimal Amount { get; set; }
    public string? Aciklama { get; set; }
    public string TotpCode { get; set; } = string.Empty;
}
