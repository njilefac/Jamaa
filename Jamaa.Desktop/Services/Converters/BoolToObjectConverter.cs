using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Jamaa.Desktop.Services.Converters;

public class BoolToObjectConverter : IValueConverter
{
    public object? TrueValue { get; set; }
    public object? FalseValue { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b) return b ? TrueValue : FalseValue;

        return FalseValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            if (TrueValue is bool tb && tb == b) return true;
            if (FalseValue is bool fb && fb == b) return false;

            // Handle string values from XAML (e.g. TrueValue="True")
            if (TrueValue is string ts && bool.TryParse(ts, out var tsb) && tsb == b) return true;
            if (FalseValue is string fs && bool.TryParse(fs, out var fsb) && fsb == b) return false;
        }

        if (Equals(value, TrueValue)) return true;
        if (Equals(value, FalseValue)) return false;

        return false;
    }
}