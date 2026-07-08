namespace BankCoreApi.Entities;

public class DefterKayit
{
    public Guid Id { get; set; }
    public Guid HesapId { get; set; }
    public Guid IslemGrupId { get; set; }
    public decimal Amount { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
