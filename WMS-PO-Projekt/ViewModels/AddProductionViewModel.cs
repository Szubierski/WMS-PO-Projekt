using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WmsProdukcja.DataAccess;
using WmsProdukcja.Models;

namespace WmsProdukcja.ViewModels;


// Logika dla widoku AddProductionPage
public partial class AddProductionViewModel : BaseViewModel
{
    private readonly WmsRepository _repository;

    [ObservableProperty]
    private DateTime dataRealizacji = DateTime.Now.AddDays(7);

    [ObservableProperty]
    private string uwagi;

    public AddProductionViewModel(WmsRepository repository)
    {
        _repository = repository;
    }
    
    // Zapisywanie nagłówna zlecenia do bazy danych
    [RelayCommand]
    public async Task Zapisz()
    {
        try
        {
            var noweZlecenie = new Produkcja
            {
                DataUtworzenia = DateTime.Now,
                TerminRealizacji = DataRealizacji,
                Status = "Zaplanowane",
                Uwagi = Uwagi
            };

            if (noweZlecenie.Uwagi == null)
            {
                throw new NullReferenceException("Pole 'Uwagi' musi posiadać opis");
            }

            await Task.Run(() => _repository.DodajTylkoNaglowekProdukcji(noweZlecenie));

            await Shell.Current.DisplayAlert("Sukces", "Utworzono nowe zlecenie. Teraz możesz dodać pozycje.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Błąd", ex.Message, "OK");
        }
    }
}