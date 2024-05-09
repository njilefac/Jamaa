using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Libota.Application.Setup;
using Libota.Application.Users;
using Libota.Application.Users.Services;
using Libota.Desktop.Assets.Resources;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;

namespace Libota.Desktop.ViewModels.Setup
{
    public class CreateSuperUserViewModel : ReactiveValidationObject, IRoutableViewModel
    {
        private readonly ISetupService _setupService;
        private readonly IUserSessionService _userSessionService;
        [Reactive] public string FirstName { get; set; }
        [Reactive] public string LastName { get; set; }
        [Reactive] public string UserName { get; set; }
        [Reactive] public string Email { get; set; }
        [Reactive] public string Password { get; set; }

        public ReactiveCommand<Unit, UserSession?> CreateAccount { get; set; }

        public CreateSuperUserViewModel(IScreen hostScreen, ISetupService userManagementFacade, IUserSessionService userSessionService)
        {
            _setupService = userManagementFacade;
            _userSessionService = userSessionService;
            HostScreen = hostScreen;

            UserName = string.Empty;
            Password = string.Empty;
            Email = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;

            this.ValidationRule(p => p.UserName,
                v => !string.IsNullOrWhiteSpace(v) && v.Length >= 3, string.Format(Messages.login_error_username, 3));

            this.ValidationRule(p => p.Password,
                v => !string.IsNullOrWhiteSpace(v) && v.Length >= 6, string.Format(Messages.login_error_password, 6));

            this.ValidationRule(p => p.FirstName,
                v => !string.IsNullOrWhiteSpace(v), Messages.login_error_username);

            this.ValidationRule(p => p.LastName,
                v => !string.IsNullOrWhiteSpace(v), Messages.login_error_username);

            CreateAccount = ReactiveCommand.CreateFromTask(OnCreateAccount, this.IsValid());
        }

        private async Task<UserSession?> OnCreateAccount()
        {
            var user = await _setupService.CreateSuperUser(UserName, Password, Email, FirstName, LastName);
            if (user?.Account.Credentials == null) return null;
            var defaultOrganisation = (await _setupService.ListOrganisations()).FirstOrDefault();
            return await _userSessionService.Authenticate(user.Account.Credentials, defaultOrganisation);

        }

        public string UrlPathSegment => "setup.create-super-user";
        public IScreen HostScreen { get; }
    }
}