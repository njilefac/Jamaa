using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Jamaa.Desktop.Services.Converters;

public class IncrementConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue) return intValue + 1;

        if (value is long longValue) return longValue + 1;

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue) return intValue - 1;

        if (value is long longValue) return longValue - 1;

        return value;
    }
}