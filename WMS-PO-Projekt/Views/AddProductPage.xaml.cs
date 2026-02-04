using WmsProdukcja.ViewModels;

namespace WmsProdukcja.Views;

public partial class AddProductPage : ContentPage
{
    public AddProductPage(AddProductViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}