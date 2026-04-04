using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Jamaa.Desktop.Services.Converters;

public class HalfWidthConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width)
        {
            return width / 2;
        }
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
