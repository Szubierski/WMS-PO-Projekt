using WmsProdukcja.ViewModels;
namespace WmsProdukcja.Views;


public partial class ProductionDetailsPage : ContentPage
{
    public ProductionDetailsPage(ProductionDetailsViewModel vm)
    {
        InitializeComponent();
        
        BindingContext = vm;
    }
}