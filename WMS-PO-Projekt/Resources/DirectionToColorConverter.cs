using System.Globalization;

namespace WmsProdukcja.Converters;

// Klasa odpowiada za zmianę koloru tekstu w zależności od wartości (IN - zielony, OUT - czerwony)
public class DirectionToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string kierunek)
        {
            return kierunek.ToUpper() == "IN" ? Colors.Green : Colors.Red;
        }
        return Colors.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}