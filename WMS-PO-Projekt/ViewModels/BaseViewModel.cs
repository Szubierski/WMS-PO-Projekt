using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage; 

namespace WmsProdukcja.ViewModels;


// Bazowa klasa z której dziedziczą wszystkie viewModels, dziedziczy po ObservableObject co daje wszyskim klasom potomnym
// dostep do ObsevablePropery oraz RelayCommand

// "partial" jest wymagany przez CommunityToolkit
public partial class BaseViewModel : ObservableObject
{
    
    [ObservableProperty]
    private bool isLoading;
    
    // Klasa odpoweidzialna za uruchomienie eksportu
    protected async Task UruchomProcesEksportu(string tytulOkna, Func<Task<string>> funkcjaGenerujacaPlik)
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            
            string sciezkaPliku = await Task.Run(funkcjaGenerujacaPlik);
            
            bool czyOtworzyc = await Shell.Current.DisplayAlert(
                "Eksport Zakończony",
                $"Zapisano plik:\n{Path.GetFileName(sciezkaPliku)}\n\n" +
                $"Czy chcesz go teraz otworzyć?",
                "Tak, otwórz", "Nie");

            if (czyOtworzyc)
            {
                await Launcher.Default.OpenAsync(new OpenFileRequest
                {
                    Title = tytulOkna,
                    File = new ReadOnlyFile(sciezkaPliku)
                });
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Błąd Eksportu", ex.Message, "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }
}