using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Jamaa.Data.Models.Members;
using Jamaa.Desktop.Members.Components;
using Jamaa.Desktop.Members.Messages;
using Jamaa.Desktop.Members.ViewModels;

namespace Jamaa.Desktop.Services.Converters;

public class MemberProfileCommandArgsConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 1)
        {
            return null;
        }

        MemberData? memberData = null;

        if (values[0] is MemberData data)
        {
            memberData = data;
        }
        else if (values[0] is MemberViewModel vm)
        {
            memberData = MemberListViewModel.MapToData(vm);
        }

        if (memberData == null)
        {
            return null;
        }
        
        var tab = values.Count >= 2 ? values[1] as string : null;
        return new MemberProfileNavigationArgs(memberData, tab);

    }
}
