using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using JetBrains.Annotations;
using Libota.Application.Setup;
using Libota.Application.Users.Services;
using Libota.Desktop.Assets.Resources;

namespace Libota.Desktop.Setup;

[UsedImplicitly]
public partial class CreateSuperUserViewModel : ObservableValidator
{
    public CreateSuperUserViewModel(
        ISetupService setupService,
        IUserSessionService userSessionService)
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
    [ObservableProperty] private object? _active;

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
    private async Task CreateSuperUser()
    {
        var user = await _setupService.CreateSuperUser(UserName, Password, Email, FirstName, LastName);
        if (user?.Account.Credentials == null) return ;
        var defaultOrganisation = (await _setupService.ListOrganisations()).FirstOrDefault();
        _ =  await _userSessionService.Authenticate(user.Account.Credentials, defaultOrganisation);
        WeakReferenceMessenger.Default.Send(new SuperUserCreated());
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