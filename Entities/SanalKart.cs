namespace BankCoreApi.Entities;

public class SanalKart
{
    public Guid Id { get; set; }
    public Guid HesapId { get; set; }
    public string KartAdi { get; set; } = string.Empty;
    public decimal AylikLimit { get; set; }
    public decimal Bakiye { get; set; }
    public string KartNoSifreli { get; set; } = string.Empty;
    public int SonKullanmaAy { get; set; }
    public int SonKullanmaYil { get; set; }
    public string CvvSifreli { get; set; } = string.Empty;
    public string Durum { get; set; } = "Aktif";
}
