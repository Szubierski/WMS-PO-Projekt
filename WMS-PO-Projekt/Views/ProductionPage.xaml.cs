using WmsProdukcja.ViewModels;

namespace WmsProdukcja.Views;

public partial class ProductionPage : ContentPage
{
    public ProductionPage(ProductionViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ProductionViewModel vm)
        {
            await vm.ZaladujZlecenia();
        }
    }
}