namespace BankCoreApi.Controllers.Dtos;

public class TransferIstek
{
    public string AliciHesapNo { get; set; } = string.Empty;
    public decimal Miktar { get; set; }
    public string TotpKodu { get; set; } = string.Empty;
}
