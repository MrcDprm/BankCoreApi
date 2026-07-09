namespace BankCoreApi.Entities;

public class Kredi
{
    public Guid Id { get; set; }
    public Guid HesapId { get; set; }
    public decimal AnaPara { get; set; }
    public decimal FaizOrani { get; set; }
    public string KrediTuru { get; set; } = string.Empty;
    public string KrediAltTuru { get; set; } = string.Empty;
    public decimal AylikTaksit { get; set; }
    public decimal AylikFaizOrani { get; set; }
    public decimal ToplamBorc { get; set; }
    public decimal KalanBorc { get; set; }
    public DateTime VadeTarihi { get; set; }
    public DateTime? SonrakiTaksitTarihi { get; set; }
    public string Durum { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
