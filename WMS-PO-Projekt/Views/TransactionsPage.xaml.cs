using WmsProdukcja.ViewModels;

namespace WmsProdukcja.Views;

public partial class TransactionsPage : ContentPage
{
    public TransactionsPage(TransactionsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var vm = BindingContext as TransactionsViewModel;
        if (vm != null)
        {
            await vm.ZaladujTransakcjeCommand.ExecuteAsync(null);
        }
    }
}