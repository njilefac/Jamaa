using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using Libota.Application.Setup;
using Libota.Application.Users;
using Libota.Application.Users.Services;
using Libota.Desktop.Navigation;
using Libota.Desktop.ViewModels.Security;
using Libota.Desktop.ViewModels.Shared;

namespace Libota.Desktop.ViewModels.Setup
{
    [UsedImplicitly]
    public partial class CreateSuperUserViewModel : ObservableValidator
    {
        public CreateSuperUserViewModel(INavigationService navigationService,  
            ISetupService setupService,
            IUserSessionService userSessionService,
            DashboardViewModel dashboardViewModel, 
            LoginScreenViewModel loginScreenViewModel)
        {
            _setupService = setupService;
            _userSessionService = userSessionService;

            UserName = string.Empty;
            Password = string.Empty;
            Email = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;

            // this.ValidationRule(p => p.UserName,
            //     v => !string.IsNullOrWhiteSpace(v) && v.Length >= 3, string.Format(Messages.login_error_username, 3));
            //
            // this.ValidationRule(p => p.Password,
            //     v => !string.IsNullOrWhiteSpace(v) && v.Length >= 6, string.Format(Messages.login_error_password, 6));
            //
            // this.ValidationRule(p => p.FirstName,
            //     v => !string.IsNullOrWhiteSpace(v), Messages.login_error_username);
            //
            // this.ValidationRule(p => p.LastName,
            //     v => !string.IsNullOrWhiteSpace(v), Messages.login_error_username);
            //
            // CreateAccount = ReactiveCommand.CreateFromTask(OnCreateAccount, this.IsValid());
        }

        [ObservableProperty] private string _firstName;
        [ObservableProperty] private string _lastName;
        [ObservableProperty] private string _userName;
        [ObservableProperty] private string _email;
        [ObservableProperty] private string _password;

        [RelayCommand]
        private async Task<UserSession?> OnCreateAccount(CancellationToken cancellationToken)
        {
            var user = await _setupService.CreateSuperUser(UserName, Password, Email, FirstName, LastName);
            if (user?.Account.Credentials == null) return null;
            var defaultOrganisation = (await _setupService.ListOrganisations()).FirstOrDefault();
            return await _userSessionService.Authenticate(user.Account.Credentials, defaultOrganisation);
        }

        private readonly ISetupService _setupService;
        private readonly IUserSessionService _userSessionService;
    }
}