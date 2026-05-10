using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Jamaa.Desktop.Services.Converters;

public class DecimalToFormattedStringConverter : IValueConverter, IMultiValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal decimalValue)
        {
            var precision = 2;
            if (parameter is int intParam)
            {
                precision = intParam;
            }
            else if (parameter is string stringParam && int.TryParse(stringParam, out var parsedPrecision))
            {
                precision = parsedPrecision;
            }

            return decimalValue.ToString($"F{precision}", culture);
        }

        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string stringValue && decimal.TryParse(stringValue, NumberStyles.Any, culture, out var decimalValue))
        {
            return decimalValue;
        }

        return BindingOperations.DoNothing;
    }

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 1 && values[0] is decimal decimalValue)
        {
            var precision = 2;
            if (values.Count >= 2 && values[1] is int intParam)
            {
                precision = intParam;
            }

            return decimalValue.ToString($"F{precision}", culture);
        }

        return values.FirstOrDefault()?.ToString();
    }

    public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        if (value is string stringValue && decimal.TryParse(stringValue, NumberStyles.Any, culture, out var decimalValue))
        {
            return [decimalValue, BindingOperations.DoNothing];
        }

        return [0m, BindingOperations.DoNothing];
    }
}
