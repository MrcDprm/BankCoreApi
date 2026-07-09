using BankCoreApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankCoreApi.Data;

public class BankaDbContext : DbContext
{
    public BankaDbContext(DbContextOptions<BankaDbContext> options) : base(options)
    {
    }

    public DbSet<Hesap> Hesaplar { get; set; }
    public DbSet<DefterKayit> DefterKayitlar { get; set; }
    public DbSet<Kredi> Krediler { get; set; }
    public DbSet<SanalKart> SanalKartlar { get; set; }
    public DbSet<KayitliAlici> KayitliAlicilar { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Hesap>(entity =>
        {
            entity.HasIndex(e => e.HesapNo).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.HesapNo).HasMaxLength(20);
            entity.Property(e => e.HesapSahibiAd).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.SifreHash).HasMaxLength(255);
        });

        modelBuilder.Entity<DefterKayit>(entity =>
        {
            entity.Property(e => e.Aciklama).HasMaxLength(255);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Kredi>(entity =>
        {
            entity.Property(e => e.AnaPara).HasPrecision(18, 2);
            entity.Property(e => e.FaizOrani).HasPrecision(18, 2);
            entity.Property(e => e.AylikTaksit).HasPrecision(18, 2);
            entity.Property(e => e.AylikFaizOrani).HasPrecision(18, 6);
            entity.Property(e => e.ToplamBorc).HasPrecision(18, 2);
            entity.Property(e => e.KalanBorc).HasPrecision(18, 2);
            entity.Property(e => e.KrediTuru).HasMaxLength(20);
            entity.Property(e => e.KrediAltTuru).HasMaxLength(50);
            entity.Property(e => e.Durum).HasMaxLength(50);
        });

        modelBuilder.Entity<SanalKart>(entity =>
        {
            entity.Property(e => e.KartAdi).HasMaxLength(100);
            entity.Property(e => e.AylikLimit).HasPrecision(18, 2);
            entity.Property(e => e.Bakiye).HasPrecision(18, 2);
            entity.Property(e => e.KartNoSifreli).HasMaxLength(512);
            entity.Property(e => e.CvvSifreli).HasMaxLength(256);
            entity.Property(e => e.Durum).HasMaxLength(50);
        });

        modelBuilder.Entity<KayitliAlici>(entity =>
        {
            entity.Property(e => e.Iban).HasMaxLength(20);
            entity.Property(e => e.KayitliAd).HasMaxLength(100);
            entity.HasIndex(e => new { e.HesapId, e.KarsiHesapId }).IsUnique();
        });
    }
}
