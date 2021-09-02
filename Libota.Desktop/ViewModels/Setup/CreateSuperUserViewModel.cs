using System.Reactive;
using System.Threading.Tasks;
using Libota.Application;
using Libota.Desktop.Assets.Resources;
using Libota.Desktop.ViewModels.Security;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using Splat;

namespace Libota.Desktop.ViewModels.Setup
{
    public class CreateSuperUserViewModel : ReactiveValidationObject, IRoutableViewModel
    {
        private readonly IUserManagementFacade _userManagementFacade;
        [Reactive] public string FirstName { get; set; }
        [Reactive] public string LastName { get; set; }
        [Reactive] public string UserName { get; set; }
        [Reactive] public string Email { get; set; }
        [Reactive] public string Password { get; set; }

        public ReactiveCommand<Unit, bool> CreateAccount { get; set; }

        public CreateSuperUserViewModel(IScreen hostScreen, IUserManagementFacade userManagementFacade)
        {
            _userManagementFacade = userManagementFacade;
            HostScreen = hostScreen;

            UserName = string.Empty;
            Password = string.Empty;
            Email = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;

            if (_userManagementFacade.GetSuperUser().Result is { } superUser)
            {
                var loginScreenViewModel = Locator.Current.GetService<LoginScreenViewModel>();
                if (loginScreenViewModel != null) HostScreen.Router.Navigate.Execute(loginScreenViewModel);
            }

            this.ValidationRule(p => p.UserName,
                v => !string.IsNullOrWhiteSpace(v) && v.Length >= 3, Messages.login_error_username);
            
            this.ValidationRule(p => p.Password,
                v => !string.IsNullOrWhiteSpace(v) && v.Length >= 6, Messages.login_error_password);
            
            this.ValidationRule(p => p.FirstName,
                v => !string.IsNullOrWhiteSpace(v), Messages.login_error_username);
            
            this.ValidationRule(p => p.LastName,
                v => !string.IsNullOrWhiteSpace(v), Messages.login_error_username);

            CreateAccount = ReactiveCommand.CreateFromTask(OnCreateAccount, this.IsValid());
        }

        private async Task<bool> OnCreateAccount()
        {
            var user = await _userManagementFacade.CreateSuperUser(UserName, Email, Password, FirstName, LastName);
            return user != null;
        }


        public string? UrlPathSegment => "setup.create-super-user";
        public IScreen HostScreen { get; }
    }
}