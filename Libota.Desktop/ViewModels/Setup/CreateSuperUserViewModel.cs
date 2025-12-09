using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using Libota.Application.Setup;
using Libota.Application.Users;
using Libota.Application.Users.Services;
using Libota.Desktop.Assets.Resources;
using Libota.Desktop.Navigation;
using Libota.Desktop.ViewModels.Security;
using Libota.Desktop.ViewModels.Shared;

namespace Libota.Desktop.ViewModels.Setup;

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
    }

    [ObservableProperty] private string _firstName;
    [ObservableProperty] private string _lastName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateSuperUserCommand))]
    [MinLength(3, ErrorMessageResourceName = "login_error_username", ErrorMessageResourceType = typeof(Messages))]
    private string _userName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateSuperUserCommand))]
    [EmailAddress(ErrorMessageResourceName = "setup_error_email", ErrorMessageResourceType = typeof(Messages))]
    private string _email;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateSuperUserCommand))]
    [MinLength(8, ErrorMessageResourceName = "login_error_password", ErrorMessageResourceType = typeof(Messages))]
    private string _password;

    [RelayCommand(CanExecute = nameof(IsValid))]
    private async Task<UserSession?> CreateSuperUser()
    {
        var user = await _setupService.CreateSuperUser(UserName, Password, Email, FirstName, LastName);
        if (user?.Account.Credentials == null) return null;
        var defaultOrganisation = (await _setupService.ListOrganisations()).FirstOrDefault();
        return await _userSessionService.Authenticate(user.Account.Credentials, defaultOrganisation);
    }

    public bool IsValid
    {
        get
        {
            ValidateProperty(UserName, nameof(UserName));
            ValidateProperty(Password, nameof(Password));
            ValidateProperty(Email, nameof(Email));
            return !HasErrors;
        }
    }

    private readonly ISetupService _setupService;
    private readonly IUserSessionService _userSessionService;
}