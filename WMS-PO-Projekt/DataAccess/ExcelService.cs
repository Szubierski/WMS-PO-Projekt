using ClosedXML.Excel;
using WmsProdukcja.Models;

namespace WmsProdukcja.DataAccess;

// Do tej klasy dodajemy metody odpowiadające za generowanie plików excel do exportu 
// oraz obługę importu
public class ExcelService
{
    // Generowanie raportu stanów magazynowcyh
    public async Task<string> GenerujRaportExcel(List<Produkt> produkty)
    {
        // Tworzenie Workbook
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Stan Magazynowy");

        // Tworzenie nagłówków
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Nazwa Produktu";
        worksheet.Cell(1, 3).Value = "Typ";
        worksheet.Cell(1, 4).Value = "Ilość";
        worksheet.Cell(1, 5).Value = "Data Utworzenia";

        // Stylizacja nagłówka
        var headerRow = worksheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Wypełnianie danymi
        for (int i = 0; i < produkty.Count; i++)
        {
            var produkt = produkty[i];
            int row = i + 2; // Zaczynamy od 2 wiersza bo 1 to nagłówek

            worksheet.Cell(row, 1).Value = produkt.IdProduktu;
            worksheet.Cell(row, 2).Value = produkt.Nazwa;
            worksheet.Cell(row, 3).Value = produkt.TypProduktu;
            
            // Obsługa nulli dla ilości
            decimal ilosc = produkt.StanMagazynowy?.AktualnaIlosc ?? 0;
            worksheet.Cell(row, 4).Value = ilosc;

            worksheet.Cell(row, 5).Value = produkt.DataUtworzenia?.ToString("yyyy-MM-dd");
        }
        worksheet.Columns().AdjustToContents();
        
        // Zapisujemy w folderze Cache aplikacji (inna lokalizacja powodowała u mnie problem z uprawnieniami dlatego na potrzeby projektu zapisuje w cacheu apki)
        string fileName = $"Raport_WMS_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

        workbook.SaveAs(filePath);

        return filePath; // zwracamy ścieżkę do pliku
    }
    
    // Generowanie raportu produkcji
    public async Task<string> GenerujRaportProdukcji(Produkcja produkcja, IList<PozycjaProdukcji> pozycje)
    {
        using var workbook = new XLWorkbook();
        // Tworzenie nazwy arkusza np Zlecenie 1
        var worksheet = workbook.Worksheets.Add($"Zlecenie {produkcja.IdProdukcji}");
        
        worksheet.Cell(1, 1).Value = "Raport Produkcji nr:";
        worksheet.Cell(1, 2).Value = produkcja.IdProdukcji;
        
        worksheet.Cell(2, 1).Value = "Status:";
        worksheet.Cell(2, 2).Value = produkcja.Status;

        worksheet.Cell(3, 1).Value = "Termin Realizacji:";
        worksheet.Cell(3, 2).Value = produkcja.TerminRealizacji.ToString("dd.MM.yyyy");

        worksheet.Cell(4, 1).Value = "Uwagi:";
        worksheet.Cell(4, 2).Value = produkcja.Uwagi ?? "Brak";

        // Stylizacja nagłówka zlecenia (pogrubienie etykiet)
        var infoRange = worksheet.Range("A1:A4");
        infoRange.Style.Font.Bold = true;
        infoRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
        worksheet.Column(1).Width = 20;
        
        int tableHeaderRow = 6;
        
        worksheet.Cell(tableHeaderRow, 1).Value = "Nazwa Produktu";
        worksheet.Cell(tableHeaderRow, 2).Value = "Ilość Planowana";
        worksheet.Cell(tableHeaderRow, 3).Value = "Typ Produktu";

        // Stylizacja nagłówka tabeli
        var tableHeader = worksheet.Row(tableHeaderRow);
        tableHeader.Style.Font.Bold = true;
        tableHeader.Style.Font.FontColor = XLColor.White;
        tableHeader.Style.Fill.BackgroundColor = XLColor.DarkGray;

        // Wypełnianie danymi
        for (int i = 0; i < pozycje.Count; i++)
        {
            var poz = pozycje[i];
            int row = tableHeaderRow + 1 + i;

            worksheet.Cell(row, 1).Value = poz.Produkt?.Nazwa ?? "Nieznany";
            worksheet.Cell(row, 2).Value = poz.IloscDocelowa;
            worksheet.Cell(row, 3).Value = poz.Produkt?.TypProduktu ?? "-";
        }

        // Dopasowanie szerokości kolumn
        worksheet.Columns().AdjustToContents();

        // Zapisujemy w folderze Cache aplikacji
        string fileName = $"Produkcja_{produkcja.IdProdukcji}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

        workbook.SaveAs(filePath);

        return filePath;
    }
    
    public List<ImportItem> ImportujDaneTransakcji(string sciezkaDoPliku)
    {
        var lista = new List<ImportItem>();

        using var workbook = new XLWorkbook(sciezkaDoPliku);
        var worksheet = workbook.Worksheet(1);

        var rows = worksheet.RangeUsed().RowsUsed();

        // Zakładam że 1. wiersz to nagłówke więc zaczynamy od 2
        foreach (var row in rows.Skip(1))
        {
            try
            {
                var item = new ImportItem
                {
                    NazwaProduktu = row.Cell(1).GetValue<string>(),
                    
                    Ilosc = row.Cell(2).GetValue<decimal>(),
                    
                    Cena = row.Cell(3).IsEmpty() ? 0 : row.Cell(3).GetValue<decimal>()
                };

                // Wlidacja pustych wierszy
                if (!string.IsNullOrWhiteSpace(item.NazwaProduktu))
                {
                    lista.Add(item);
                }
            }
            catch 
            {
                // Ignoruje błędne wiersze (ale można tutaj dodać ich wylapywanie)
            }
        }

        return lista;
    }
}

public class ImportItem
{
    public string NazwaProduktu { get; set; }
    public decimal Ilosc { get; set; }
    public decimal Cena { get; set; }
}