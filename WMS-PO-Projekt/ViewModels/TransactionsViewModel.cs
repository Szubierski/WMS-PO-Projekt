using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WmsProdukcja.DataAccess;
using WmsProdukcja.Models;
using WmsProdukcja.Views;   

namespace WmsProdukcja.ViewModels;

// Logika dla TransactionsPage
public partial class TransactionsViewModel : BaseViewModel
{
    private readonly WmsRepository _repository;
    public ObservableCollection<Transakcja> Transakcje { get; } = new();

    public TransactionsViewModel(WmsRepository repository)
    {
        _repository = repository;
    }

    // Ładujemy listę transakcji
    [RelayCommand]
    public async Task ZaladujTransakcje()
    {
        IsLoading = true;
        var dane = await Task.Run(() => _repository.PobierzWszystkieTransakcje());
        Transakcje.Clear();
        foreach (var t in dane) Transakcje.Add(t);
        IsLoading = false;
    }
    
    // Obsługa przejścai do okna dodawania transakcji
    [RelayCommand]
    public async Task PrzejdzDoDodawania() => await Shell.Current.GoToAsync(nameof(AddTransactionPage));
}
