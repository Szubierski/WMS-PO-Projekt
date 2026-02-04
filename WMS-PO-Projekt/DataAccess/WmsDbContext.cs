using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WmsProdukcja.Models;

namespace WmsProdukcja.DataAccess;

// Klasa odpowiedzialna za mapowanie tabel oraz relacji (framework Microsoft.EntityFrameworkCore)
public class WmsDbContext : DbContext
{
    // Konfiguracja tabel
    public DbSet<Produkt> Produkty { get; set; }
    public DbSet<Surowiec> Surowce { get; set; }
    public DbSet<Produkcja> Produkcje { get; set; }
    public DbSet<PozycjaProdukcji> PozycjeProdukcji { get; set; }
    public DbSet<ZuzycieSurowca> ZuzyteSurowce { get; set; }
    
    public DbSet<StanMagazynowy> StanyMagazynowe { get; set; }
    public DbSet<ProduktSpecyfikacja> Specyfikacje { get; set; }
    
    public DbSet<Transakcja> Transakcje { get; set; }
    public DbSet<PozycjaTransakcji> PozycjeTransakcji { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Konfiguracja połączenia z bazą danych
        var connectionString = "server=localhost;user=root;password=;database=wms_produkcja_db";

        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
            .LogTo(Console.WriteLine, LogLevel.Information)
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Produkt>(entity =>
        {
            // Mapowanie 
            entity.HasDiscriminator<string>("TypProduktu")
                .HasValue<Surowiec>("Surowiec Rolka")
                .HasValue<Arkusz>("Wyrób Gotowy Arkusz")
                .HasValue<Worek>("Wyrób Gotowy Worek");

            // Relacja 1:1 ze Stanem Magazynowym
            entity.HasOne(p => p.StanMagazynowy)
                .WithOne(s => s.Produkt)
                .HasForeignKey<StanMagazynowy>(s => s.IdProduktu);

            // Relacja 1:N ze Specyfikacjami
            entity.HasMany(p => p.Specyfikacje)
                .WithOne()
                .HasForeignKey(s => s.IdProduktu);
        });
        
        modelBuilder.Entity<ProduktSpecyfikacja>(entity =>
        {
            entity.ToTable("PRODUKT_SPECYFIKACJA");
            entity.Property(e => e.IdProduktu).HasColumnName("id_produktu");
        });
        
        // Pozycja produckji
        modelBuilder.Entity<PozycjaProdukcji>(entity =>
        {
            entity.ToTable("POZYCJA_PRODUKCJI");
            entity.HasKey(e => e.IdPozycji);

            // Mapowanie kolumn
            entity.Property(e => e.IdPozycji).HasColumnName("id_pozycji");
            entity.Property(e => e.IdProdukcji).HasColumnName("id_produkcji");
            entity.Property(e => e.IdProduktu).HasColumnName("id_produktu");
            entity.Property(e => e.IloscDocelowa).HasColumnName("ilosc_docelowa");
            entity.Property(e => e.DataRealizacji).HasColumnName("data_realizacji");

            // Relacje
            entity.HasOne(d => d.Produkcja)
                .WithMany(p => p.Pozycje)
                .HasForeignKey(d => d.IdProdukcji);

            entity.HasOne(d => d.Produkt)
                .WithMany()
                .HasForeignKey(d => d.IdProduktu);
        });

        // Relacja surowca
        modelBuilder.Entity<ZuzycieSurowca>(entity =>
        {
            entity.ToTable("ZUZYCIE_SUROWCA");
            entity.HasKey(e => e.IdZuzycia);

            // Mapowanie kolumn
            entity.Property(e => e.IdZuzycia).HasColumnName("id_zuzycia");
            entity.Property(e => e.IdProdukcji).HasColumnName("id_produkcji");
            entity.Property(e => e.IdProduktu).HasColumnName("id_produktu");
            entity.Property(e => e.IloscZuzycia).HasColumnName("ilosc_zuzycia");

            // Relacje
            entity.HasOne(d => d.Produkcja)
                .WithMany(p => p.ZuzyteSurowce)
                .HasForeignKey(d => d.IdProdukcji);

            entity.HasOne(d => d.Produkt)
                .WithMany()
                .HasForeignKey(d => d.IdProduktu);
        });
    }
}