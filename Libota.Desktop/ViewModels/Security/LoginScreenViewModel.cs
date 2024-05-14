using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Domain.Security.Values;
using Libota.Application.Setup;
using Libota.Application.Users;
using Libota.Application.Users.Services;
using Libota.Data.Models.Organisation;
using Libota.Desktop.Assets.Resources;
using Libota.Desktop.ViewModels.Shared;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using Splat;

namespace Libota.Desktop.ViewModels.Security
{
    public class LoginScreenViewModel : ReactiveValidationObject, IRoutableViewModel
    {
        private readonly IUserSessionService? _userSessionService;

        [Reactive] public string? UserName { get; set; }

        [Reactive] public string? Password { get; set; }

        [Reactive] public IList<OrganisationData>? Organisations { get; set; }

        [Reactive] public OrganisationData? CurrentOrganisation { get; set; }
        public ReactiveCommand<Unit, UserSession?> Login { get; }

        public Interaction<UserSession?, Unit> NotifyAuthenticationResult { get; set; }

        public LoginScreenViewModel(IScreen screen, IUserSessionService userSessionService, ISetupService setupService, ILogger<LoginScreenViewModel> logger)
        {
            HostScreen = screen;
            _userSessionService = userSessionService;
            
            _userSessionService.UserSessions.Subscribe(x =>
            {
                if (x is not { IsAuthenticated: true }) return;
                var nextViewModel = Locator.Current.GetService<DashboardViewModel>();
                HostScreen.Router.Navigate.Execute(nextViewModel!);
            });

            UserName = string.Empty;
            Password = string.Empty;

            setupService.ListOrganisations().ContinueWith(task =>
            {
                Organisations = task.Result.ToList();
                CurrentOrganisation = Organisations?.FirstOrDefault();
            });

            this.ValidationRule(vm => vm.UserName,
                x => !string.IsNullOrEmpty(x) && x.Length >= 3,
                string.Format(Messages.login_error_username, 3));

            this.ValidationRule(vm => vm.Password,
                x => !string.IsNullOrEmpty(x) && x.Length >= 6,
                string.Format(Messages.login_error_password, 6));

            NotifyAuthenticationResult = new Interaction<UserSession?, Unit>();

            Login = ReactiveCommand.CreateFromTask(AuthenticateUser, this.IsValid());

            Login.ThrownExceptions.Subscribe(ex => { logger.LogError(ex, "login error {Exception}", ex.Message); });
        }

        private async Task<UserSession?> AuthenticateUser()
        {
            var credentials = new Credentials(UserName, Password);
            var response = await _userSessionService?.Authenticate(credentials, CurrentOrganisation)!;
            return response;
        }

        public string UrlPathSegment => "login";
        public IScreen HostScreen { get; }
    }
}