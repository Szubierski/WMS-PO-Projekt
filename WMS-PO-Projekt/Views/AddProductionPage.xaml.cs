using WmsProdukcja.ViewModels;

namespace WmsProdukcja.Views;

public partial class AddProductionPage : ContentPage
{
    public AddProductionPage(AddProductionViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}