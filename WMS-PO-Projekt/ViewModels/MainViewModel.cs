using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Data;
using WmsProdukcja.DataAccess;
using WmsProdukcja.Models;

namespace WmsProdukcja.ViewModels;


// viewModel głównej strony (stanów magazynowych)
public partial class MainViewModel : BaseViewModel
{
    private readonly WmsRepository _repository;
    private readonly ExcelService _excelService;
    
    // ObservableCollection to lista która odświeża View przy jej modyfikacji
    [ObservableProperty]
    private ObservableCollection<Produkt> produkty;

    [ObservableProperty]
    private bool isLoading; // Loader poczas pobierania danych
    
    public MainViewModel(WmsRepository repository, ExcelService excelService)
    {
        _repository = repository;
        _excelService = excelService;
        Produkty = new ObservableCollection<Produkt>();
    }

    // [RelayCommand] zamienia zwykłą metodę w Komendę, którą można podpiąć pod przycisk w XAML.
    [RelayCommand]
    public async Task ZaladujDane()
    {
        IsLoading = true;
        try
        {
            // Symulowane sztuczne opóźnienie (dla pokazania jak działą loader)
            await Task.Delay(500); 

            // Pobieranie danych w tle
            var daneZBazy = await Task.Run(() => _repository.PobierzWszystkieProdukty());

            Produkty.Clear();
        
            if (daneZBazy.Count == 0)
            {
                // Jeśli lista jest pusta wyrzucamy błąd
                throw new NoNullAllowedException("Połączono z bazą, ale tabela PRODUKT jest pusta.");
            }

            foreach (var produkt in daneZBazy)
            {
                Produkty.Add(produkt);
            }
        }
        catch (Exception ex)
        {
            // Wyświetlamy błąd na ekranie
            await Shell.Current.DisplayAlert("Błąd Bazy Danych", $"Szczegóły błędu: {ex.Message}.", 
                "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    
    [RelayCommand]
    public async Task IdzDoDodawania()
    {
        await Shell.Current.GoToAsync(nameof(Views.AddProductPage));
    }
    
    [RelayCommand]
    public async Task UsunProdukt(Produkt produktDoUsuniecia)
    {
        if (produktDoUsuniecia == null) return;

        // 1. Pytamy użytkownika o potwierdzenie
        bool czyPotwierdzic = await Shell.Current.DisplayAlert(
            "Potwierdzenie", 
            $"Czy na pewno chcesz usunąć produkt: {produktDoUsuniecia.Nazwa}?\n" +
            $"Tej operacji nie można cofnąć.", 
            "Tak, usuń", 
            "Anuluj");

        if (!czyPotwierdzic) return;

        try
        {
            // 2. Usuwamy z Bazy Danych
            await Task.Run(() => _repository.UsunProdukt(produktDoUsuniecia.IdProduktu));

            // 3. Usuwamy z Listy na ekranie
            Produkty.Remove(produktDoUsuniecia);

            // Komunikat o pomyślnym usunięciu produktu
            await Shell.Current.DisplayAlert("Info", "Produkt usunięty", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Błąd", $"Nie udało się usunąć: {ex.Message}", "OK");
        }
    }
    
    
    // Wywołanie exportu excel
    [RelayCommand]
    public async Task EksportujExcel()
    {
        if (Produkty.Count == 0)
        {
            await Shell.Current.DisplayAlert("Pusto", "Brak danych do eksportu.", "OK");
            return;
        }
        
        await UruchomProcesEksportu("Wyeksportuj Stan Magazynowy", async () => 
        {
            // Uruchamiamy odpowiednią funkcję z ExcelService która zwraca ścieżkę do pliku
            return await _excelService.GenerujRaportExcel(Produkty.ToList());
        });
    }
    
    // Wywołanie importu excel
    [RelayCommand]
    public async Task ImportujExcel()
    {
        try
        {
            // Definiujemy jakie typy plików mogą być importowane
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            { 
                { DevicePlatform.WinUI, new[] { ".xlsx" } },
                { DevicePlatform.macOS, new[] {".xlsx"} }
            });
            
            // Wybór pliku z komputera
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Wybierz plik Excel z dostawą",
                FileTypes = customFileType // Tylko Excel
            });

            if (result == null) return; // Nie wybrano pliku więc anulowano

            IsLoading = true;

            // Odczyt danych z pliku
            var sciezka = result.FullPath;
            var zaimportowaneDane = await Task.Run(() => _excelService.ImportujDaneTransakcji(sciezka));

            if (zaimportowaneDane.Count == 0)
            {
                await Shell.Current.DisplayAlert("Błąd", "Plik jest pusty lub ma zły format.", "OK");
                return;
            }
            
            // Sprawdzamy, czy wszystkie produkty z Excela istnieją w bazie
            var nowaTransakcja = new Transakcja
            {
                TypTransakcji = "Przyjęcie zewnętrzne",
                DataTransakcji = DateTime.Now,
                Uwagi = $"Import z pliku: {result.FileName}",
                Pozycje = new List<PozycjaTransakcji>()
            };

            List<string> bledy = new();
            int numerWiersza = 2; // Bo 1 to nagłówek

            foreach (var item in zaimportowaneDane)
            {
                var produktWBazie = _repository.PobierzProduktPoNazwie(item.NazwaProduktu);

                if (produktWBazie == null)
                {
                    bledy.Add($"Wiersz {numerWiersza}: Nie znaleziono produktu '{item.NazwaProduktu}'");
                }
                else
                {
                    // Produkt istnieje - dodajemy do transakcji
                    nowaTransakcja.Pozycje.Add(new PozycjaTransakcji
                    {
                        IdProduktu = produktWBazie.IdProduktu,
                        IloscZmiany = item.Ilosc,
                        Kierunek = "IN", // Przyjęcie wchodzi na magazyn
                    });
                }
                numerWiersza++;
            }

            // Sprawdzmay czy są błedy
            if (bledy.Any())
            {
                // Wyświetla listę błędów i przerywa
                string komunikat = string.Join("\n", bledy.Take(10)); // Pokaż max 10 błędów
                if (bledy.Count > 10) komunikat += "\n...i więcej.";
                
                throw new Exception($"{komunikat}.\n\n Popraw plik");
                return;
            }

            // Zapisujemy do bazy
            await Task.Run(() => _repository.ZaksiegujTransakcje(nowaTransakcja));
            
            // Odświeżamy listę produktów na ekranie
            await ZaladujDane();

            await Shell.Current.DisplayAlert("Sukces", "Zaimportowano dostawę i zaktualizowano stany!", "OK");

        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Błąd krytyczny", ex.Message, "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }
}