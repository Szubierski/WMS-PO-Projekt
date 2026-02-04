using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Data;
using WmsProdukcja.DataAccess;
using WmsProdukcja.Models;

namespace WmsProdukcja.ViewModels;

// Logika dla widoku AddTransactionPage
public partial class AddTransactionViewModel : BaseViewModel
{
    private readonly WmsRepository _repository;

    [ObservableProperty] private string typTransakcji; // np "Korekta" "Dostawa"
    [ObservableProperty] private string uwagi;
    [ObservableProperty] private Produkt wybranyProdukt;
    [ObservableProperty] private decimal ilosc;
    [ObservableProperty] private string wybranyKierunek = "IN";

    public List<string> Kierunki { get; } = new() { "IN", "OUT" };
    public ObservableCollection<Produkt> Produkty { get; } = new();

    public AddTransactionViewModel(WmsRepository repository)
    {
        _repository = repository;
        LadowanieProduktow();
    }

    // Ładowanie produktow
    private void LadowanieProduktow()
    {
        var lista = _repository.PobierzWszystkieProdukty();
        foreach (var p in lista) Produkty.Add(p);
    }

    
    // Zapisujemy transakcję
    [RelayCommand]
    public async Task ZapiszTransakcje()
    {
        try
        {
            // Sprawdzamy czy wszystkie pola są uzupełnione
            if (WybranyProdukt == null || Ilosc <= 0 || string.IsNullOrWhiteSpace(TypTransakcji))
            {
                throw new NoNullAllowedException("Wypełnij wszystkie pola!");
            }

            // Tworzenie nowej transakcji
            var transakcja = new Transakcja
            {
                TypTransakcji = TypTransakcji,
                DataTransakcji = DateTime.Now,
                Uwagi = Uwagi,
                Pozycje = new List<PozycjaTransakcji>
                {
                    // Tworzenie pozycji transakcji
                    new PozycjaTransakcji
                    {
                        IdProduktu = WybranyProdukt.IdProduktu,
                        IloscZmiany = Ilosc,
                        Kierunek = WybranyKierunek
                    }
                }
            };

            await Task.Run(() => _repository.ZaksiegujTransakcje(transakcja));
            await Shell.Current.DisplayAlert("Sukces", "Transakcja została zapisana.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Błąd", ex.Message, "OK");
        }
    }
}