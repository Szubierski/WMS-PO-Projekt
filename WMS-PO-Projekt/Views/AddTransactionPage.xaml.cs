using WmsProdukcja.ViewModels;

namespace WmsProdukcja.Views;
public partial class AddTransactionPage : ContentPage
{
    public AddTransactionPage(AddTransactionViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}