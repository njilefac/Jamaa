using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using FluentAvalonia.UI.Controls;

namespace Jamaa.Desktop.Services.Converters;

public class JamaaIconSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string iconKey && !string.IsNullOrWhiteSpace(iconKey) &&
            targetType.IsAssignableTo(typeof(FAIconSource)))
            return Get<FAIconSource>(iconKey);

        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static T? Get<T>(string resourceName)
    {
        try
        {
            var success = Avalonia.Application.Current!.TryGetResource(resourceName,
                Avalonia.Application.Current.ActualThemeVariant, out var outValue);

            if (success && outValue is T value) return value;

            return default;
        }
        catch
        {
            return default;
        }
    }
}