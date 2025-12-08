using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Libota.Desktop.Infrastructure;

public static class ServiceProviderExtensions
{
    public static Control? GetViewForViewModel<TVm>(this IServiceProvider serviceProvider)
        where TVm : ObservableObject
    {
        var service = serviceProvider.GetRequiredService(typeof(IViewFor<TVm>)) as Control;
        return service;
    }
}