using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WmsProdukcja.Models;

// Klasa Bazowa Produkt
[Table("PRODUKT")]
public abstract class Produkt
{
    [Key]
    [Column("id_produktu")]
    public int IdProduktu { get; set; }

    [Column("nazwa")]
    [Required]
    public string Nazwa { get; set; } = string.Empty;

    [Column("typ_produktu")]
    public string TypProduktu { get; set; } = string.Empty;

    [Column("data_utworzenia")]
    public DateTime? DataUtworzenia { get; set; }

    public StanMagazynowy? StanMagazynowy { get; set; }
    public ICollection<ProduktSpecyfikacja> Specyfikacje { get; set; } = new List<ProduktSpecyfikacja>();
}

// Klasa Surowiec
public class Surowiec : Produkt { }

public class Arkusz : Produkt { }

public class Worek : Produkt { }

// Klasa Produkcji
[Table("PRODUKCJA")]
public class Produkcja
{
    [Key]
    [Column("id_produkcji")]
    public int IdProdukcji { get; set; }

    [Column("data_utworzenia")]
    public DateTime? DataUtworzenia { get; set; }

    [Column("termin_realizacji")]
    public DateTime TerminRealizacji { get; set; }

    [Column("status")]
    [Required]
    public string Status { get; set; } = "Zaplanowane";

    [Column("uwagi")]
    public string? Uwagi { get; set; }

    public ICollection<PozycjaProdukcji> Pozycje { get; set; } = new List<PozycjaProdukcji>();
    public ICollection<ZuzycieSurowca> ZuzyteSurowce { get; set; } = new List<ZuzycieSurowca>();
}

// Pozycja Produkcji
[Table("POZYCJA_PRODUKCJI")]
public class PozycjaProdukcji
{
    [Key] [Column("id_pozycji")] public int IdPozycji { get; set; }
    
    [Column("id_produkcji")] public int IdProdukcji { get; set; }
    public Produkcja? Produkcja { get; set; } // Bez [ForeignKey]

    [Column("id_produktu")] public int IdProduktu { get; set; }
    public Produkt? Produkt { get; set; }     // Bez [ForeignKey]

    [Column("ilosc_docelowa")] public decimal IloscDocelowa { get; set; }
    [Column("data_realizacji")] public DateTime? DataRealizacji { get; set; }
}

// Zużycie surowca
[Table("ZUZYCIE_SUROWCA")]
public class ZuzycieSurowca
{
    [Key] [Column("id_zuzycia")] public int IdZuzycia { get; set; }
    
    [Column("id_produkcji")] public int IdProdukcji { get; set; }
    public Produkcja? Produkcja { get; set; } // Bez [ForeignKey]

    [Column("id_produktu")] public int IdProduktu { get; set; }
    public Produkt? Produkt { get; set; }     // Bez [ForeignKey]
    
    [Column("ilosc_zuzycia")] public decimal IloscZuzycia { get; set; }
}

// Stan Magazynowy
[Table("STAN_MAGAZYNOWY")]
public class StanMagazynowy
{
    [Key]
    [Column("id_stanu")]
    public int IdStanu { get; set; }

    [Column("id_produktu")]
    public int IdProduktu { get; set; }
    public Produkt? Produkt { get; set; }

    [Column("aktualna_ilosc")]
    public decimal AktualnaIlosc { get; set; }

    [Column("data_ostatniej_zmiany")]
    public DateTime? DataOstatniejZmiany { get; set; }
}

// Specyfikacja
[Table("PRODUKT_SPECYFIKACJA")]
public class ProduktSpecyfikacja
{
    [Key]
    [Column("id_specyfikacji")]
    public int IdSpecyfikacji { get; set; }

    [Column("id_produktu")]
    public int IdProduktu { get; set; }

    [Column("grubosc_mm")]
    public decimal? GruboscMm { get; set; }

    [Column("szerokosc_m")]
    public decimal? SzerokoscM { get; set; }

    [Column("dlugosc_m")]
    public decimal? DlugoscM { get; set; }

    [Column("material")]
    public string Material { get; set; }
}

// Transakcja
[Table("TRANSAKCJA")]
public class Transakcja
{
    [Key] [Column("id_transakcji")] public int IdTransakcji { get; set; }
    [Column("typ_transakcji")] public string TypTransakcji { get; set; } = string.Empty;
    [Column("data_transakcji")] public DateTime? DataTransakcji { get; set; }
    [Column("uwagi")] public string? Uwagi { get; set; }

    public ICollection<PozycjaTransakcji> Pozycje { get; set; } = new List<PozycjaTransakcji>();
}

// Pozycja trransakcji
[Table("POZYCJA_TRANSAKCJI")]
public class PozycjaTransakcji
{
    [Key] [Column("id_pozycji")] public int IdPozycji { get; set; }
    [Column("id_transakcji")] public int IdTransakcji { get; set; }
    
    [ForeignKey("IdTransakcji")]
    public Transakcja? Transakcja { get; set; }

    [Column("id_produktu")] public int IdProduktu { get; set; }
    
    [ForeignKey("IdProduktu")]
    public Produkt? Produkt { get; set; }

    [Column("ilosc_zmiany")] public decimal IloscZmiany { get; set; }
    [Column("kierunek")] public string Kierunek { get; set; } = "IN"; 
}