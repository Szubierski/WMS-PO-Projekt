using Microsoft.EntityFrameworkCore;
using WmsProdukcja.Models;

namespace WmsProdukcja.DataAccess;

// Operacje na bazie danych
public class WmsRepository
{
    // Pobieranie wszystkich produktów
    public List<Produkt> PobierzWszystkieProdukty()
    {
        using (var context = new WmsDbContext())
        {
            return context.Produkty
                          .Include(p => p.StanMagazynowy)
                          .Include(p => p.Specyfikacje)
                          .ToList();
        }
    }
    
    // Dodanie produktu ze specyfikajcą
    public void DodajProduktZeSpecyfikacja(Produkt nowyProdukt, ProduktSpecyfikacja spec)
    {
        using (var context = new WmsDbContext())
        {
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    // Dodajemy produkt i inicjujemy stan
                    if (nowyProdukt.StanMagazynowy == null)
                    {
                        nowyProdukt.StanMagazynowy = new StanMagazynowy 
                        { 
                            AktualnaIlosc = 0,
                            DataOstatniejZmiany = DateTime.Now 
                        };
                    }
                    context.Produkty.Add(nowyProdukt);
                    context.SaveChanges(); // Tu generuje się IdProduktu

                    // Przypisujemy ID produktu do specyfikacji i zapisujemy
                    spec.IdProduktu = nowyProdukt.IdProduktu;
                    context.Specyfikacje.Add(spec);
                    context.SaveChanges();

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }

    // Usuwanie produktu
    public void UsunProdukt(int idProduktu)
    {
        using (var context = new WmsDbContext())
        {
            // Najpierw szukamy produktu w bazie.
            var produktDoUsuniecia = context.Produkty.Find(idProduktu);
            
            if (produktDoUsuniecia != null)
            {
                context.Produkty.Remove(produktDoUsuniecia);
                context.SaveChanges(); 
            }
        }
    }
    
    // Aktualizacja statusu produkcji
    public void AktualizujStatusProdukcji(int idProdukcji, string nowyStatus)
    {
        using (var context = new WmsDbContext())
        {
            var produkcja = context.Produkcje.Find(idProdukcji);
            if (produkcja != null)
            {
                produkcja.Status = nowyStatus;
                context.SaveChanges();
            }
        }
    }
    
    // Metoda do pobierania zleceń produkcyjnych
    public List<Produkcja> PobierzZlecenia()
    {
        using (var context = new WmsDbContext())
        {
            return context.Produkcje
                .Include(p => p.Pozycje)
                .ThenInclude(pos => pos.Produkt) 
                .OrderBy(p => p.TerminRealizacji)
                .ToList();
        }
    }
    
    // Dodawanie produkcji (bez pozycji)
    public void DodajTylkoNaglowekProdukcji(Produkcja produkcja)
    {
        using (var context = new WmsDbContext())
        {
            context.Produkcje.Add(produkcja);
            context.SaveChanges();
        }
    }
    
    // Usuwanie produkcji
    public void UsunProdukcje(int idProdukcji)
    {
        using var context = new WmsDbContext();
        
        var produkcja = context.Produkcje
            .Include(p => p.Pozycje)
            .Include(p => p.ZuzyteSurowce) 
            .FirstOrDefault(p => p.IdProdukcji == idProdukcji);

        if (produkcja != null)
        {
            // Sprawdzamy status dla bezpieczeństwa (nawet jeśli przycisk jest ukryty)
            if (produkcja.Status == "Zakończone")
                throw new Exception("Nie można usunąć produkcji, która została już zakończona.");

            // Usuwamy powiązane pozycje i planowane zużycie
            context.PozycjeProdukcji.RemoveRange(produkcja.Pozycje);
            if (produkcja.ZuzyteSurowce != null)
                context.ZuzyteSurowce.RemoveRange(produkcja.ZuzyteSurowce);

            // Usuwamy nagłówek produkcji
            context.Produkcje.Remove(produkcja);
            context.SaveChanges();
        }
    }
    
    // Pobieranie produkcji po jej id
    public Produkcja PobierzProdukcjePoId(int idProdukcji)
    {
        using (var context = new WmsDbContext())
        {
            return context.Produkcje
                .Include(p => p.Pozycje)
                .ThenInclude(pos => pos.Produkt)
                .FirstOrDefault(p => p.IdProdukcji == idProdukcji);
        }
    }
    
    // Plan zurzycia surowca podczas produkcji
    public List<ZuzycieSurowca> PobierzPlanowaneZuzycieDlaProdukcji(int idProdukcji)
    {
        using var context = new WmsDbContext();
        return context.ZuzyteSurowce
            .Include(z => z.Produkt)
            .Where(z => z.IdProdukcji == idProdukcji) // Filtrujemy po ID całego zlecenia
            .ToList();
    }

    // Lista zleceń w produkcji
    public List<PozycjaProdukcji> PobierzPozycjeDlaProdukcji(int idProdukcji)
    {
        using (var context = new WmsDbContext())
        {
            return context.PozycjeProdukcji
                .Include(pp => pp.Produkt) // Żeby pobrać nazwę produktu
                .Where(pp => pp.IdProdukcji == idProdukcji)
                .ToList();
        }
    }
    // Pobieranie produktu po jego id
    public Produkt PobierzProduktPoId(int id)
    {
        using (var context = new WmsDbContext())
        {
            return context.Produkty.FirstOrDefault(p => p.IdProduktu == id);
        }
    }
    // Metoda księgująca całą transakcję (tworzy historię + zmienia stan magazynu)
    public void ZaksiegujTransakcje(Transakcja transakcja)
    {
        using (var context = new WmsDbContext())
        {
            using (var dbTrans = context.Database.BeginTransaction())
            {
                try
                {
                    // Nagłówek transakcji
                    context.Transakcje.Add(transakcja);
                    context.SaveChanges();

                    // Przetwarzamy pozycje i aktualizujemy stany
                    foreach (var pozycja in transakcja.Pozycje)
                    {
                        // Znajdujemy stan magazynowy tego produktu
                        var stan = context.StanyMagazynowe
                            .FirstOrDefault(s => s.IdProduktu == pozycja.IdProduktu);
                        
                        if (pozycja.Kierunek == "OUT" && stan.AktualnaIlosc < pozycja.IloscZmiany)
                        {
                            throw new Exception($"Niewystarczająca ilość produktu {stan.Produkt.Nazwa} na magazynie!");
                        }
                        if (stan != null)
                        {
                            // Jeśli typ to "Przyjęcie" to Dodajemy
                            // Jeśli "Wydanie" to Odejmujemy
                            if (pozycja.Kierunek == "IN")
                                stan.AktualnaIlosc += pozycja.IloscZmiany;
                            else
                                stan.AktualnaIlosc -= pozycja.IloscZmiany;

                            stan.DataOstatniejZmiany = DateTime.Now;
                        }
                    }

                    context.SaveChanges();
                    dbTrans.Commit(); // Zatwierdzamy wszystko
                }
                catch
                {
                    dbTrans.Rollback(); // Wycofujemy jak coś poszło nie tak
                    throw;
                }
            }
        }
    }

    // Pomocnicza do szukania ID po nazwie (do walidacji importu)
    public Produkt? PobierzProduktPoNazwie(string nazwa)
    {
        using (var context = new WmsDbContext())
        {
            return context.Produkty.FirstOrDefault(p => p.Nazwa == nazwa);
        }
    }
    
    public WynikWalidacjiSurowca WalidujZapotrzebowanieNaSurowiec(int idProduktuWyrobu, decimal iloscWyrobu)
    {
        using var context = new WmsDbContext();
        var wynik = new WynikWalidacjiSurowca();

        // Pobierz specyfikację wyrobu gotowego
        var specWyrobu = context.Specyfikacje.FirstOrDefault(s => s.IdProduktu == idProduktuWyrobu);
        var produktWyrobu = context.Produkty.Find(idProduktuWyrobu);
        if (specWyrobu == null) return new WynikWalidacjiSurowca { CzyDostepny = false, Komunikat = "Brak specyfikacji technicznej wyrobu." };

        List<Produkt> pasujaceSurowce = new();

        // LOGIKA DLA WORKA
        if (produktWyrobu is Worek)
        {
            decimal szukanaSzerokoscRolki = (specWyrobu.SzerokoscM ?? 0) * 2;
            pasujaceSurowce = context.Surowce
                .Include(s => s.Specyfikacje)
                .Include(s => s.StanMagazynowy)
                .Where(s => s.Specyfikacje.Any(sp => sp.GruboscMm == specWyrobu.GruboscMm && sp.SzerokoscM == szukanaSzerokoscRolki))
                .Cast<Produkt>().ToList();
        }
        // LOGIKA DLA ARKUSZA
        else if (produktWyrobu is Arkusz)
        {
            // Szukamy rolek o grubości wyrobu i szerokości równej szerokości LUB długości arkusza
            pasujaceSurowce = context.Surowce
                .Include(s => s.Specyfikacje)
                .Include(s => s.StanMagazynowy)
                .Where(s => s.Specyfikacje.Any(sp => sp.GruboscMm == specWyrobu.GruboscMm && 
                       (sp.SzerokoscM == specWyrobu.SzerokoscM || sp.SzerokoscM == specWyrobu.DlugoscM)))
                .Cast<Produkt>().ToList();
        }

        // Obliczanie wydajnosci (Sumaryczna długość rolek)
        decimal potrzebnaSztuk = iloscWyrobu;
        decimal sumaSztukZMagazynu = 0;

        foreach (var sur in pasujaceSurowce.OrderByDescending(s => s.StanMagazynowy.AktualnaIlosc))
        {
            var specSur = sur.Specyfikacje.First();
            
            // Ile sztuk wyjdzie z jednej rolki (długość rolki / długość wyrobu)
            decimal dlugoscWyrobu = (produktWyrobu is Arkusz && specSur.SzerokoscM == specWyrobu.DlugoscM) 
                                    ? (specWyrobu.SzerokoscM ?? 1) : (specWyrobu.DlugoscM ?? 1);
            
            decimal sztukZRolki = Math.Floor((specSur.DlugoscM ?? 0) / dlugoscWyrobu);
            if (sztukZRolki <= 0) continue;

            decimal potrzebneRolkiTegoTypu = Math.Ceiling(potrzebnaSztuk / sztukZRolki);
            decimal dostepneRolki = sur.StanMagazynowy?.AktualnaIlosc ?? 0;

            decimal uzyteRolki = Math.Min(potrzebneRolkiTegoTypu, dostepneRolki);
            
            if (uzyteRolki > 0)
            {
                wynik.PlanowaneZuzycie.Add(new ZuzycieSurowca {
                    IdProduktu = sur.IdProduktu,
                    IloscZuzycia = uzyteRolki
                });
                sumaSztukZMagazynu += uzyteRolki * sztukZRolki;
                potrzebnaSztuk -= (uzyteRolki * sztukZRolki);
            }

            if (potrzebnaSztuk <= 0) break;
        }

        wynik.CzyDostepny = potrzebnaSztuk <= 0;
        wynik.Komunikat = wynik.CzyDostepny ? "Surowiec dostępny." : "Niewystarczająca ilość surowca na magazynie.";
        return wynik;
    }
    
    public void ZakonczProdukcje(int idProdukcji)
    {
        using var context = new WmsDbContext();
        using var dbTransaction = context.Database.BeginTransaction();

        try
        {
            var produkcja = context.Produkcje
                .Include(p => p.Pozycje).ThenInclude(pos => pos.Produkt)
                .Include(p => p.ZuzyteSurowce).ThenInclude(z => z.Produkt)
                .FirstOrDefault(p => p.IdProdukcji == idProdukcji);

            if (produkcja == null || produkcja.Status == "Zakończone") return;

            // Nagłówek transakcji historycznej
            var transakcja = new Transakcja {
                TypTransakcji = "Rozliczenie Produkcji",
                DataTransakcji = DateTime.Now,
                Uwagi = $"Zlecenie nr {idProdukcji}"
            };
            context.Transakcje.Add(transakcja);
            context.SaveChanges();

            // Zdjęcie SUROWCÓW (OUT)
            foreach (var zuzycie in produkcja.ZuzyteSurowce)
            {
                var stan = context.StanyMagazynowe.FirstOrDefault(s => s.IdProduktu == zuzycie.IdProduktu);
                if (stan != null) stan.AktualnaIlosc -= zuzycie.IloscZuzycia;

                context.PozycjeTransakcji.Add(new PozycjaTransakcji {
                    IdTransakcji = transakcja.IdTransakcji,
                    IdProduktu = zuzycie.IdProduktu,
                    IloscZmiany = zuzycie.IloscZuzycia,
                    Kierunek = "OUT"
                });
            }

            // Przyjęcie WYROBÓW GOTOWYCH (IN)
            foreach (var poz in produkcja.Pozycje)
            {
                var stan = context.StanyMagazynowe.FirstOrDefault(s => s.IdProduktu == poz.IdProduktu);
                if (stan != null) stan.AktualnaIlosc += poz.IloscDocelowa;

                context.PozycjeTransakcji.Add(new PozycjaTransakcji {
                    IdTransakcji = transakcja.IdTransakcji,
                    IdProduktu = poz.IdProduktu,
                    IloscZmiany = poz.IloscDocelowa,
                    Kierunek = "IN"
                });
            }

            produkcja.Status = "Zakończone";
            context.SaveChanges();
            dbTransaction.Commit();
        }
        catch
        {
            dbTransaction.Rollback();
            throw;
        }
    }
    
    public void ZapiszPozycjeZPlanemSurowcow(PozycjaProdukcji pozycja, List<ZuzycieSurowca> planowaneZuzycie)
    {
        using var context = new WmsDbContext();
        using var dbTransaction = context.Database.BeginTransaction();

        try
        {
            // Zapisujemy pozycję produkcji (wyrobu gotowego)
            pozycja.Produkt = null; 
            context.PozycjeProdukcji.Add(pozycja);
            context.SaveChanges();

            // Zapisujemy planowane zużycie surowców
            foreach (var zuzycie in planowaneZuzycie)
            {
                zuzycie.IdProdukcji = pozycja.IdProdukcji;
                zuzycie.Produkt = null; 
                context.ZuzyteSurowce.Add(zuzycie);
            }

            context.SaveChanges();
        
            dbTransaction.Commit();
        }
        catch (Exception ex)
        {
            dbTransaction.Rollback();
            throw new Exception("Błąd podczas zapisywania planu produkcji i surowców: " + ex.Message);
        }
    }
    
    public List<Transakcja> PobierzWszystkieTransakcje()
    {
        using var context = new WmsDbContext();
        return context.Transakcje
            .Include(t => t.Pozycje)
            .ThenInclude(p => p.Produkt)
            .OrderByDescending(t => t.DataTransakcji)
            .ToList();
    }
    
    public WynikWalidacjiSurowca WalidujIZarezerwujSurowiec(int idProduktuWyrobu, decimal iloscWyrobu)
    {
        using var context = new WmsDbContext();
        var wynik = new WynikWalidacjiSurowca();

        // Pobierz specyfikację wyrobu
        var specWyrobu = context.Specyfikacje.FirstOrDefault(s => s.IdProduktu == idProduktuWyrobu);
        var produktWyrobu = context.Produkty.Find(idProduktuWyrobu);

        if (specWyrobu == null) return new WynikWalidacjiSurowca { CzyDostepny = false, Komunikat = "Brak danych technicznych." };

        List<Produkt> pasujaceSurowce = new();

        // LOGIKA WYBORU ROLKI
        if (produktWyrobu is Worek)
        {
            decimal szukanaSzerokosc = (specWyrobu.SzerokoscM ?? 0) * 2;
            pasujaceSurowce = context.Surowce
                .Include(s => s.Specyfikacje)
                .Include(s => s.StanMagazynowy)
                .Where(s => s.Specyfikacje.Any(sp => sp.GruboscMm == specWyrobu.GruboscMm && sp.SzerokoscM == szukanaSzerokosc))
                .Cast<Produkt>() // <--- DODAJ TO
                .ToList();
        }
        // LOGIKA DLA ARKUSZA
        else if (produktWyrobu is Arkusz)
        {
            pasujaceSurowce = context.Surowce
                .Include(s => s.Specyfikacje)
                .Include(s => s.StanMagazynowy)
                .Where(s => s.Specyfikacje.Any(sp => sp.GruboscMm == specWyrobu.GruboscMm && 
                                                     (sp.SzerokoscM == specWyrobu.SzerokoscM || sp.SzerokoscM == specWyrobu.DlugoscM)))
                .Cast<Produkt>() // <--- DODAJ TO
                .ToList();
        }

        // OBLICZENIE I REZERWACJA (Sumaryczna długość)
        decimal pozostałoDoWyprodukowania = iloscWyrobu;
        
        // Sortujemy surowce tak, aby najpierw brać te, których jest więcej na stanie
        foreach (var sur in pasujaceSurowce.OrderByDescending(s => s.StanMagazynowy.AktualnaIlosc))
        {
            var specSur = sur.Specyfikacje.First();
            
            // Określenie wydajności z 1 rolki
            decimal wymiarWyrobuWzdlozRolki = (produktWyrobu is Arkusz && specSur.SzerokoscM == specWyrobu.DlugoscM) 
                                              ? (specWyrobu.SzerokoscM ?? 1) : (specWyrobu.DlugoscM ?? 1);
            
            decimal sztukZRolki = Math.Floor((specSur.DlugoscM ?? 0) / wymiarWyrobuWzdlozRolki);
            if (sztukZRolki <= 0) continue;

            decimal potrzebneRolki = Math.Ceiling(pozostałoDoWyprodukowania / sztukZRolki);
            decimal dostepneRolki = sur.StanMagazynowy.AktualnaIlosc;

            decimal uzyteRolki = Math.Min(potrzebneRolki, dostepneRolki);
            
            if (uzyteRolki > 0)
            {
                wynik.PlanowaneZuzycie.Add(new ZuzycieSurowca {
                    IdProduktu = sur.IdProduktu,
                    IloscZuzycia = uzyteRolki
                });
                pozostałoDoWyprodukowania -= (uzyteRolki * sztukZRolki);
            }

            if (pozostałoDoWyprodukowania <= 0) break;
        }

        wynik.CzyDostepny = pozostałoDoWyprodukowania <= 0;
        wynik.Komunikat = wynik.CzyDostepny ? "OK" : "Brak wystarczającej ilości pasujących rolek na stanie.";
        return wynik;
    }
}

public class WynikWalidacjiSurowca
{
    public bool CzyDostepny { get; set; }
    public List<ZuzycieSurowca> PlanowaneZuzycie { get; set; } = new();
    public string Komunikat { get; set; } = string.Empty;
}