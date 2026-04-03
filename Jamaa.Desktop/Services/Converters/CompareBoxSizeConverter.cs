using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Jamaa.Desktop.Dashboard;

namespace Jamaa.Desktop.Services.Converters;

public class CompareBoxSizeConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2) return false;
        if (values[0] == null || values[1] == null) return false;

        // Try to convert both values to the same enum type for comparison
        try
        {
            var val1 = values[0] is BoxSize s1 ? s1 : (BoxSize)System.Convert.ToInt32(values[0]);
            var val2 = values[1] is BoxSize s2 ? s2 : (BoxSize)System.Convert.ToInt32(values[1]);
            
            return val1 == val2;
        }
        catch
        {
            return false;
        }
    }
}
