using WmsProdukcja.Views; 

namespace WmsProdukcja;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        // Rejestracja wszystkich przekierowań
        Routing.RegisterRoute(nameof(AddProductPage), typeof(AddProductPage));
        Routing.RegisterRoute(nameof(AddProductionPage), typeof(AddProductionPage));
        Routing.RegisterRoute(nameof(ProductionDetailsPage), typeof(ProductionDetailsPage));
        Routing.RegisterRoute(nameof(TransactionsPage), typeof(TransactionsPage));
        Routing.RegisterRoute(nameof(AddTransactionPage), typeof(AddTransactionPage));
    }
}