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
    }
}
