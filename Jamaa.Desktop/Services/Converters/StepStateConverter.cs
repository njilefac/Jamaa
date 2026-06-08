using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Jamaa.Desktop.Services.Converters;

public class StepStateConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 3) return "Uncompleted";

        var isCompleted = values[0] is bool b1 && b1;
        var isSelected = values[1] is bool b2 && b2;
        var isEnabled = values[2] is bool b3 && b3;

        if (!isEnabled) return "Disabled";
        if (isCompleted) return "Completed";
        if (isSelected) return "Active";

        return "Uncompleted";
    }
}
