using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Jamaa.Desktop.Services.Converters;

public class DecimalToFormattedStringConverter : IMultiValueConverter
{
    private const string DefaultThousandSeparator = ",";

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

        return values.Count > 0 ? values[0]?.ToString() : null;
    }

    private static NumberFormatInfo BuildNumberFormat(CultureInfo culture, string thousandSeparator)
    {
        var format = (NumberFormatInfo)culture.NumberFormat.Clone();
        format.NumberGroupSeparator = thousandSeparator;
        return format;
    }
}
