using System;
using Libota.Application.Users.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Libota.Desktop.ViewModels.Shared
{
    public class MainWindowViewModel : ReactiveObject, IScreen
    {
        private const string APPLICATION_NAME = "Libota Desktop";

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        [Reactive] public string? ApplicationTitle { get; set; } = APPLICATION_NAME;
        public RoutingState Router { get; }

        public MainWindowViewModel(IUserSessionService userSessionService)
        {
            userSessionService.UserSessions.Subscribe(x =>
            {
                ApplicationTitle = x is { IsAuthenticated: true } ? $"{APPLICATION_NAME} -  ({x.UserName})" : APPLICATION_NAME;
            });
            Router = new RoutingState();
        }
    }
}