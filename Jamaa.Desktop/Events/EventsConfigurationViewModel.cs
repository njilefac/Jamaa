using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Events;

/// <summary>
///     Operation: Provides UI configuration and state management for events settings.
///     Currently serves as a placeholder for future events configuration options.
/// </summary>
public class EventsConfigurationViewModel : ObservableObject, IApplicationModule, INavigationHost
{
    public Guid Id => Guid.Parse("a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d");
    public string Title => "Events Configuration";
    public object? HeaderContent => null;

    public void NavigateTo<TViewModel>(object? parameter = null)
    {
    }

    public void NavigateTo(string route, object? parameter = null)
    {
    }

    public bool CanGoBack()
    {
        return false;
    }

    public void GoBack()
    {
    }

    public bool CanGoForward()
    {
        return false;
    }

    public void GoForward()
    {
    }
}