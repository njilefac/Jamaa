using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Libota.Application.Users.Services;
using Libota.Desktop.ViewModels.Security;
using ReactiveUI;
using Splat;
using static System.Threading.Tasks.Task;

namespace Libota.Desktop.ViewModels.Shared
{
    public class MainMenuViewModel : ReactiveObject
    {
        private readonly IUserSessionService _sessionService;
        private readonly IScreen _hostScreen;

        public ReactiveCommand<Unit, Unit> Exit { get; }
        public ReactiveCommand<Unit, Unit> Logout { get; }
        public ReactiveCommand<Unit, Unit> Edit { get; }

        public MainMenuViewModel(IUserSessionService sessionService, IScreen hostScreen)
        {
            _sessionService = sessionService;
            _hostScreen = hostScreen;
            
            var userIsAuthenticated = _sessionService.UserSessions.Select(s => s is {IsAuthenticated: true});

            Exit = ReactiveCommand.CreateFromTask(ExitApplication);
            Logout = ReactiveCommand.CreateFromTask(EndUserSession, userIsAuthenticated);
            Edit = ReactiveCommand.CreateFromObservable<Unit>(() => null!, userIsAuthenticated);
        }

        private Task EndUserSession()
        {
            _sessionService.EndSession();
            _hostScreen.Router.Navigate.Execute(Locator.Current.GetService<LoginScreenViewModel>() ?? throw new InvalidOperationException());
            return CompletedTask;
        }

        private static Task ExitApplication()
        {
            if (Avalonia.Application.Current.ApplicationLifetime is ClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
            return CompletedTask;
        }
    }
}