using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Metadata;
using Jamaa.Desktop.Members.Components;
using Jamaa.Desktop.Members.Values;
using Serilog;

namespace Jamaa.Desktop.Members.Services;

public class MemberListTemplateSelector : IDataTemplate, IValueConverter
{
    // The dictionary of templates
    [Content] public Dictionary<MemberListDisplayMode, IDataTemplate> Templates { get; } = new();

    public Control Build(object? param)
    {
        throw new NotSupportedException("Use this class as a converter to get the actual template.");
    }

    public bool Match(object? data)
    {
        return data is MemberListViewModel;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MemberListDisplayMode mode)
        {
            if (Templates.TryGetValue(mode, out var template)) return template;

            Log.Warning("No DataTemplate registered for display mode {DisplayMode}. Falling back to Card view.", mode);

            if (mode != MemberListDisplayMode.Card &&
                Templates.TryGetValue(MemberListDisplayMode.Card, out var cardTemplate)) return cardTemplate;
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}