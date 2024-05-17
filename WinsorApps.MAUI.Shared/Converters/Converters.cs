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

public class DateTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dt)
        {
            if (parameter is string format)
                return dt.ToString(format);
            return $"{dt:dd MMMM yyyy}";
        }

        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && DateTime.TryParse(str, out var dt))
            return dt;

        return default(DateTime);
    }
}
