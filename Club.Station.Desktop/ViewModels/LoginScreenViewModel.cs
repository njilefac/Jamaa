using System;
using System.Reactive;
using System.Threading.Tasks;
using Domain.Services;
using Domain.Values;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Club.Station.Desktop.ViewModels
{
    public class LoginScreenViewModel : ReactiveObject, IRoutableViewModel
    {
        private readonly IUserSessionService? _userSessionService;

        [Reactive] public string UserName { get; set; }

        [Reactive] public string Password { get; set; }
        public ReactiveCommand<Unit, UserSession> Login { get; }

        public LoginScreenViewModel(IScreen screen,
            IUserSessionService userSessionService,
            ILogger<LoginScreenViewModel> logger
        )
        {
            _userSessionService = userSessionService;
            HostScreen = screen;

            UserName = string.Empty;
            Password = string.Empty;

            var canLogin = this.WhenAnyValue(x => x.UserName,
                x => x.Password,
                (user, pass) => user.Length > 3
                                && pass.Length > 5);

            Login = ReactiveCommand.CreateFromTask<UserSession>(
                () => AuthenticateUser(new Credentials(UserName, Password))!, canLogin);

            Login.ThrownExceptions.Subscribe(ex => { logger.LogError(ex, $"an error occured"); });
        }

        private async Task<UserSession?> AuthenticateUser(Credentials credentials)
        {
            var userSession = await _userSessionService?.Authenticate(credentials)!;
            return userSession;
        }

        public string UrlPathSegment => "login";
        public IScreen HostScreen { get; }
    }
}