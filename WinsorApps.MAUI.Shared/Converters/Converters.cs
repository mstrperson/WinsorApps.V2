using System.Globalization;
using WinsorApps.Services.Global;

namespace WinsorApps.MAUI.Shared.Converters;

public class CurrencyToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
            return $"{d:C}";

        return $"{value}";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value is string str)
        {
            return str.ConvertToCurrency();
        }

        return 0;
    }
}

public class BoolInverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b) { return !b; } 
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b) { return !b; }
        return false;
    }
}
