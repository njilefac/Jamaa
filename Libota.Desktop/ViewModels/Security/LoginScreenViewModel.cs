using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Domain.Values;
using Libota.Application.Organisation.Queries.Models;
using Libota.Application.Setup;
using Libota.Application.Users.Services;
using Libota.Desktop.Assets.Resources;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;

namespace Libota.Desktop.ViewModels.Security
{
    public class LoginScreenViewModel : ReactiveValidationObject, IRoutableViewModel
    {
        private readonly IUserSessionService? _userSessionService;

        [Reactive] public string? UserName { get; set; }

        [Reactive] public string? Password { get; set; }

        [Reactive] public IList<OrganisationReadModel>? Organisations { get; set; }
        
        [Reactive] public OrganisationReadModel? CurrentOrganisation { get; set; }
        public ReactiveCommand<Unit, UserSession> Login { get; }

        public LoginScreenViewModel(IScreen screen,
            IUserSessionService userSessionService,
            ISetupService setupService,
            ILogger<LoginScreenViewModel> logger)
        {
            _userSessionService = userSessionService;
            HostScreen = screen;

            UserName = string.Empty;
            Password = string.Empty;

            Organisations = setupService.ListOrganisations().Result.ToList();
            
            CurrentOrganisation = Organisations?.FirstOrDefault();

            this.ValidationRule(vm => vm.UserName,
                x => !string.IsNullOrEmpty(x) && x.Length >= 3,
                string.Format(Messages.login_error_username, 3));

            this.ValidationRule(vm => vm.Password,
                x => !string.IsNullOrEmpty(x) && x.Length >= 6,
                string.Format(Messages.login_error_password, 6));

            Login = ReactiveCommand.CreateFromTask<UserSession>(
                () => AuthenticateUser(new Credentials(UserName, Password))!, this.IsValid());

            Login.ThrownExceptions.Subscribe(ex => { logger.LogError(ex, "login error {Exception}", ex.Message); });
        }

        private async Task<UserSession?> AuthenticateUser(Credentials credentials)
        {
            return await _userSessionService?.Authenticate(credentials, CurrentOrganisation.Id)!;
        }

        public string UrlPathSegment => "login";
        public IScreen HostScreen { get; }
    }
}