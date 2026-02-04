using System.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WmsProdukcja.DataAccess;
using WmsProdukcja.Models;

namespace WmsProdukcja.ViewModels;

// Logika dla widoku AddProductPage
public partial class AddProductViewModel : BaseViewModel
{
    private readonly WmsRepository _repository;

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
    

    // Lista typów do wyboru w liście rozwijanej
    public List<string> TypyProduktow { get; } = new()
    {
        // Musi pasować do typu surowca w bazie
        "Surowiec Rolka",
        "Wyrób Gotowy Arkusz",
        "Wyrób Gotowy Worek"
    };

    public AddProductViewModel(WmsRepository repository)
    {
        _repository = repository;
    }
    
    // Zapisywanie produktu do bazy danych
    [RelayCommand]
    public async Task Zapisz()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Nazwa) || string.IsNullOrWhiteSpace(WybranyTyp))
            {
                throw new NoNullAllowedException("Podaj nazwę i wybierz typ produktu!");
                return;
            }
            // Sprawdzamy czy podane wymary są różne od zera
            if (Grubosc <= 0 || Szerokosc <= 0 || Dlugosc <= 0)
            {
                throw new NoNullAllowedException("Wszystkie wartości muszą być większe od 0 aby prawidłowo wyliczyć zurzycie");
            }

            Produkt nowyProdukt = WybranyTyp switch
            {
                "Surowiec Rolka" => new Surowiec(),
                "Wyrób Gotowy Arkusz" => new Arkusz(),
                "Wyrób Gotowy Worek" => new Worek(),
                _ => throw new Exception("Nieznany typ produktu")
            };

            nowyProdukt.Nazwa = Nazwa;
            nowyProdukt.TypProduktu = WybranyTyp;
            nowyProdukt.DataUtworzenia = DateTime.Now;

            // Tworzymy obiekt specyfikacji
            var specyfikacja = new ProduktSpecyfikacja
            {
                GruboscMm = Grubosc,
                SzerokoscM = Szerokosc,
                DlugoscM = Dlugosc,
                Material = Material ?? "PE"
            };

            // Wywołujemy metodę repozytorium
            await Task.Run(() => _repository.DodajProduktZeSpecyfikacja(nowyProdukt, specyfikacja));

            await Shell.Current.DisplayAlert("Sukces", "Produkt wraz ze specyfikacją został dodany!", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {  
            // Wyłapanie i obsługa błędów
            await Shell.Current.DisplayAlert("Błąd", $"Nie udało się zapisać: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task Anuluj()
    {
        // Cofa nas o jeden view
        await Shell.Current.GoToAsync("..");
    }
}