using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Jamaa.Desktop.Services.Converters;

public class DecimalToFormattedStringConverter : IValueConverter, IMultiValueConverter
{
    private const string DefaultThousandSeparator = ",";

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal decimalValue)
        {
            var precision = 2;
            var thousandSeparator = DefaultThousandSeparator;
            if (parameter is int intParam)
            {
                precision = intParam;
            }
            else if (parameter is string stringParam && int.TryParse(stringParam, out var parsedPrecision))
            {
                precision = parsedPrecision;
            }

            var numberFormat = BuildNumberFormat(culture, thousandSeparator);
            return decimalValue.ToString($"N{precision}", numberFormat);
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
            var thousandSeparator = DefaultThousandSeparator;
            if (values.Count >= 2 && values[1] is int intParam)
            {
                precision = intParam;
            }
            else if (values.Count >= 2 && values[1] is string intText && int.TryParse(intText, out var parsedPrecision))
            {
                precision = parsedPrecision;
            }

            if (values.Count >= 3 && values[2] is string separator && separator.Length == 1)
            {
                thousandSeparator = separator;
            }

            var numberFormat = BuildNumberFormat(culture, thousandSeparator);
            return decimalValue.ToString($"N{precision}", numberFormat);
        }

        return values.FirstOrDefault()?.ToString();
    }

    public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        if (value is string stringValue &&
            (decimal.TryParse(stringValue, NumberStyles.Any, culture, out var decimalValue) ||
             decimal.TryParse(RemoveGroupingSeparators(stringValue), NumberStyles.Any, culture, out decimalValue)))
        {
            var result = new object?[targetTypes.Length];
            result[0] = decimalValue;
            for (var i = 1; i < result.Length; i++) result[i] = BindingOperations.DoNothing;
            return result;
        }

        var fallback = new object?[targetTypes.Length];
        fallback[0] = 0m;
        for (var i = 1; i < fallback.Length; i++) fallback[i] = BindingOperations.DoNothing;
        return fallback;
    }

    private static NumberFormatInfo BuildNumberFormat(CultureInfo culture, string thousandSeparator)
    {
        var format = (NumberFormatInfo)culture.NumberFormat.Clone();
        format.NumberGroupSeparator = thousandSeparator;
        return format;
    }

    private static string RemoveGroupingSeparators(string value)
    {
        return value.Replace(",", string.Empty)
            .Replace(" ", string.Empty)
            .Replace("'", string.Empty)
            .Replace("_", string.Empty);
    }
}
