using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WmsProdukcja.DataAccess;
using WmsProdukcja.Models;

namespace WmsProdukcja.ViewModels;

// Logika do ProductionPage
public partial class ProductionViewModel : BaseViewModel
{
    private readonly WmsRepository _repository;

    [ObservableProperty]
    private bool isLoading;

    public ObservableCollection<Produkcja> Zlecenia { get; } = new();

    public ProductionViewModel(WmsRepository repository)
    {
        _repository = repository;
    }

    // Ładujemy zlcenia
    [RelayCommand]
    public async Task ZaladujZlecenia()
    {
        IsLoading = true;
        try
        {
            var dane = await Task.Run(() => _repository.PobierzZlecenia());
            
            Zlecenia.Clear();
            foreach (var item in dane)
            {
                Zlecenia.Add(item);
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Błąd", $"Nie udało się pobrać produkcji: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Obsługa przeniesienia do okna dodawania pozycji produkcji
    [RelayCommand]
    public async Task NoweZlecenie()
    {
        await Shell.Current.GoToAsync(nameof(Views.AddProductionPage));
    }
    
    // Obłsyga przeniesienia do okna szczegółów
    [RelayCommand]
    public async Task IdzDoSzczegolow(Produkcja wybranaProdukcja)
    {
        if (wybranaProdukcja == null) return;
        await Shell.Current.GoToAsync($"{nameof(Views.ProductionDetailsPage)}?id={wybranaProdukcja.IdProdukcji}");
    }
    
    // Obsługa usuwania produkcji (wraz z jej pozycjami)
    [RelayCommand]
    public async Task UsunProdukcje(Produkcja produkcja)
    {
        if (produkcja == null) return;

        // Potwierdzenie od użytkownika czy na pewno chce usunąć
        bool czyKontynuowac = await Shell.Current.DisplayAlert(
            "Potwierdzenie", 
            $"Czy na pewno chcesz usunąć zlecenie nr {produkcja.IdProdukcji}?", 
            "Tak", "Nie");

        if (!czyKontynuowac) return;

        try
        {
            IsLoading = true;
            await Task.Run(() => _repository.UsunProdukcje(produkcja.IdProdukcji));
        
            // Odświeżamy listę po usunięciu
            await ZaladujZlecenia(); 
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
}