using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Libota.Application.Users.Services;
using Libota.Desktop.Shared;

namespace Libota.Desktop.Events
{
    public class EventsOverviewPageViewModel(IUserSessionService userSessionService) : ObservableObject, IApplicationModule
    {
    public Guid Id => Guid.Parse("e2f9a6c0-8b4d-4c9a-9e3b-2f3c4d5e6f7a");
    public string Title => "Events";
    public object? HeaderContent { get; } = null;
    }
}