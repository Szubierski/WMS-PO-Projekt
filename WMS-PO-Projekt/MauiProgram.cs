using Microsoft.Extensions.Logging;
using WmsProdukcja.Views;
using WmsProdukcja.DataAccess;
using WmsProdukcja.ViewModels;

namespace WmsProdukcja;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif
        // Rejestracja DataAcces
        builder.Services.AddSingleton<WmsDbContext>();
        builder.Services.AddSingleton<WmsRepository>();
        builder.Services.AddSingleton<ExcelService>();
        
        // Rejestracja VeiwModel
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<AddProductViewModel>();
        builder.Services.AddTransient<ProductionViewModel>();
        builder.Services.AddTransient<AddProductionViewModel>();
        builder.Services.AddTransient<ProductionDetailsViewModel>();
        builder.Services.AddTransient<TransactionsViewModel>();
        builder.Services.AddTransient<AddTransactionViewModel>();
        
        // Rejestracja View
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<AddProductPage>();
        builder.Services.AddTransient<ProductionPage>();
        builder.Services.AddTransient<ProductionDetailsPage>();
        builder.Services.AddTransient<TransactionsPage>();
        builder.Services.AddTransient<AddTransactionPage>();
        
        return builder.Build();
    }
}