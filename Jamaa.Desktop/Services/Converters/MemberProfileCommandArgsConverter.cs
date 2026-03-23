using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Jamaa.Data.Models.Members;
using Jamaa.Desktop.Members.Messages;

namespace Jamaa.Desktop.Services.Converters;

public class MemberProfileCommandArgsConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 1 || values[0] is not MemberData member)
        {
            return null;
        }
        
        var tab = values.Count >= 2 ? values[1] as string : null;
        return new MemberProfileNavigationArgs(member, tab);

    }
}
