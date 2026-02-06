using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WmsProdukcja.DataAccess;
using WmsProdukcja.Models;

namespace WmsProdukcja.ViewModels;

// QueryProperty pozwala odebrać ID klikniętego zlecenia
[QueryProperty(nameof(IdProdukcji), "id")]

// Klasa odpowiedzialna za logikę ProductionDetailsPage
public partial class ProductionDetailsViewModel : BaseViewModel
{
    private readonly WmsRepository _repository;
    private readonly ExcelService _excelService;
    
    [ObservableProperty]
    private int idProdukcji;

    [ObservableProperty]
    private Produkcja aktualnaProdukcja;
    
    [ObservableProperty]
    private Produkt wybranyProdukt;
    
    [ObservableProperty]
    private decimal iloscDoProdukcji;
    
    [ObservableProperty]
    private string nazwa;

    [ObservableProperty]
    private string wybranyTyp;

    [ObservableProperty]
    private decimal grubosc;

    [ObservableProperty]
    private decimal szerokosc;

    [ObservableProperty]
    private decimal dlugosc;

    [ObservableProperty] 
    private string material;
    
    public ObservableCollection<PozycjaProdukcji> Pozycje { get; } = new();
    public ObservableCollection<Produkt> DostepneProdukty { get; } = new();

    public ProductionDetailsViewModel(WmsRepository repository, ExcelService excelService)
    {
        _repository = repository;
        _excelService = excelService;
        ZaladujListeProduktow();
    }

    // Ta metoda uruchomi się automatycznie, gdy MAUI przekaże IdProdukcji
    async partial void OnIdProdukcjiChanged(int value)
    {
        await ZaladujSzczegoly();
    }
    
    private void ZaladujListeProduktow()
    {
        var produkty = _repository.PobierzWszystkieProdukty();
        DostepneProdukty.Clear();
        foreach (var p in produkty)
        {
            if (p is not Surowiec) DostepneProdukty.Add(p);
        }
    }

    [RelayCommand]
    public async Task ZaladujSzczegoly()
    {
        try
        {
            if (IdProdukcji == 0)
            {
                await Shell.Current.DisplayAlert("Debug", "Błąd: IdProdukcji wynosi 0!", "OK");
                return;
            }

            var naglowek = await Task.Run(() => _repository.PobierzProdukcjePoId(IdProdukcji));
            AktualnaProdukcja = naglowek;
            
            var listaPozycji = await Task.Run(() => _repository.PobierzPozycjeDlaProdukcji(IdProdukcji));
            
            Pozycje.Clear();
            foreach (var poz in listaPozycji)
            {
                if (poz.Produkt == null)
                {
                    poz.Produkt = _repository.PobierzProduktPoId(poz.IdProduktu); 
                    
                    if (poz.Produkt == null)
                    {
                        poz.Produkt = new Arkusz { Nazwa = $"BŁĄD DANYCH (ID: {poz.IdProduktu})" };
                    }
                }
                Pozycje.Add(poz);
                AktualizujPodsumowanieSurowcow();
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Błąd Krytyczny", ex.ToString(), "OK");
        }
    }
    [RelayCommand]
    public async Task DodajPozycje()
    {
        if (WybranyProdukt == null || IloscDoProdukcji <= 0) return;
        try
        {
            IsLoading = true;
            // Walidacja surowców
            var wynik = await Task.Run(() =>
                _repository.WalidujIZarezerwujSurowiec(WybranyProdukt.IdProduktu, IloscDoProdukcji));

            if (!wynik.CzyDostepny)
            {
                await Shell.Current.DisplayAlert("Brak Surowca", wynik.Komunikat, "OK");
                return;
            }

            // Jeśli surowiec jest - zapisujemy pozycję i planowane zużycie
            var nowaPozycja = new PozycjaProdukcji
            {
                IdProdukcji = IdProdukcji,
                IdProduktu = WybranyProdukt.IdProduktu,
                IloscDocelowa = IloscDoProdukcji
            };

            await Task.Run(() => _repository.ZapiszPozycjeZPlanemSurowcow(nowaPozycja, wynik.PlanowaneZuzycie));
            await ZaladujSzczegoly();
        }
        catch (Exception ex)
        {
            Shell.Current.DisplayAlert("Błąd", $"Nie udało się dodać pozycji: {ex.Message}", "OK");
        }
        finally {
            IsLoading = false;
        }
    }
    
    // Zmiana statusu produkcji
    [RelayCommand]
    public async Task ZmienStatus()
    {
        string nowyStatus = await Shell.Current.DisplayActionSheet(
            "Zmień status produkcji na:", 
            "Anuluj", 
            null, 
            "Zaplanowane", 
            "W trakcie", 
            "Zakończone");

        if (string.IsNullOrWhiteSpace(nowyStatus) || nowyStatus == "Anuluj")
            return;

        try
        {
            IsLoading = true;

            if (nowyStatus == "Zakończone")
            {
                if (Pozycje.Count == 0)
                {
                    await Shell.Current.DisplayAlert("Błąd", "Nie można zakończyć produkcji bez dodanych produktów!", "OK");
                    return;
                }
                // Wywołujemy nową metodę z Repozytorium, która robi wszystko na raz
                await Task.Run(() => _repository.ZakonczProdukcje(IdProdukcji));
                await Shell.Current.DisplayAlert("Sukces", "Produkcja zakończona. Stany magazynowe zostały zaktualizowane.", "OK");
            }
            else
            {
                await Task.Run(() => _repository.AktualizujStatusProdukcji(IdProdukcji, nowyStatus));
            }

            await ZaladujSzczegoly(); // Odświeżamy widok
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Błąd", $"Nie udało się zmienić statusu: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    // Eksport do excela
    [RelayCommand]
    public async Task EksportujExcel()
    {
        if (AktualnaProdukcja == null) return;
        if (Pozycje.Count == 0)
        {
            await Shell.Current.DisplayAlert("Info", "Brak pozycji do eksportu.", "OK");
            return;
        }
        
        await UruchomProcesEksportu("Wyeksportuj Zlecenie", async () => 
        {
            // Uruchamiamy odpowiednią funkcję z ExcelService która zwraca ścieżkę do pliku
            return await _excelService.GenerujRaportProdukcji(AktualnaProdukcja, Pozycje.ToList());
        });
    }
    [RelayCommand]
    public async Task UsunAktualnaProdukcje()
    {
        if (AktualnaProdukcja == null) return;

        // Sprawdzamy status 
        if (AktualnaProdukcja.Status == "Zakończone")
        {
            await Shell.Current.DisplayAlert("Błąd", "Nie można usunąć zakończonej produkcji.", "OK");
            return;
        }

        // Potwierdzenie
        bool czyKontynuowac = await Shell.Current.DisplayAlert(
            "Potwierdzenie", 
            "Czy na pewno chcesz usunąć to zlecenie i wszystkie jego pozycje?", 
            "Tak", "Nie");

        if (!czyKontynuowac) return;

        try
        {
            IsLoading = true;
            // Usuwanie z bazy
            await Task.Run(() => _repository.UsunProdukcje(AktualnaProdukcja.IdProdukcji));

            // Powrót do listy
            await Shell.Current.GoToAsync(".."); 
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Błąd", ex.Message, "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }
    [ObservableProperty]
    private ObservableCollection<ZuzycieSurowca> zbiorczeZapotrzebowanie = new();
    
    // Odpowiada za wypisywanie surowców potrzebncyh do wykonania produkcji
    public void AktualizujPodsumowanieSurowcow()
    {
        ZbiorczeZapotrzebowanie.Clear();
    
        // Pobieramy wszystko co jest przypisane do tego zlecenia
        var dane = _repository.PobierzPlanowaneZuzycieDlaProdukcji(IdProdukcji);
    
        // Grupowanie, żeby nie dublować tych samych surowców na liście
        var zsumowane = dane
            .GroupBy(z => z.IdProduktu)
            .Select(g => new ZuzycieSurowca
            {
                IdProduktu = g.Key,
                Produkt = g.First().Produkt,
                IloscZuzycia = g.Sum(x => x.IloscZuzycia)
            });

        foreach (var item in zsumowane)
        {
            ZbiorczeZapotrzebowanie.Add(item);
        }
    }
}