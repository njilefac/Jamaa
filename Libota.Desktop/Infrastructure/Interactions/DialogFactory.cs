using System;
using FluentAvalonia.UI.Controls;
using Libota.Desktop.ViewModels.Members;
using Libota.Desktop.Views.Members;

namespace Libota.Desktop.Infrastructure.Interactions;

public static class DialogFactory
{
    public static ContentDialog Create<TViewModel>()
    {
        return typeof(TViewModel) switch
        {
            var t when t == typeof(MemberRegistrationViewModel)
                => new MemberRegistrationDialog(),

            _ => throw new InvalidOperationException(
                $"No dialog registered for {typeof(TViewModel)}")
        };
    }
}