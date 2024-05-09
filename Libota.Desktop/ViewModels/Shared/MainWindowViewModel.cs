using System;
using Libota.Application.Users.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Libota.Desktop.ViewModels.Shared
{
    public class MainWindowViewModel : ReactiveObject, IScreen
    {
        private const string APPLICATION_NAME = "Libota Desktop";

        [Reactive] public string? ApplicationTitle { get; set; } = APPLICATION_NAME;
        public RoutingState Router { get; }

        public MainWindowViewModel(IUserSessionService userSessionService)
        {
            userSessionService.UserSessions.Subscribe(x =>
            {
                ApplicationTitle = x is { IsAuthenticated: true } ? $"{APPLICATION_NAME} -  ({x.Organisation?.Name})" : APPLICATION_NAME;
            });
            Router = new RoutingState();
        }
    }
}