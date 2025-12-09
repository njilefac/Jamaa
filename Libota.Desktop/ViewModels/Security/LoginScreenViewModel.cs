using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Security.Values;
using JetBrains.Annotations;
using Libota.Application.Setup;
using Libota.Application.Users;
using Libota.Application.Users.Services;
using Libota.Data.Models.Organisation;
using Libota.Desktop.Assets.Resources;
using Libota.Desktop.Infrastructure;
using Libota.Desktop.Navigation;
using Libota.Desktop.ViewModels.Shared;
using Microsoft.Extensions.Logging;

namespace Libota.Desktop.ViewModels.Security;

[UsedImplicitly]
public partial class LoginScreenViewModel : ObservableValidator
{
    public LoginScreenViewModel(INavigationService navigationService,
        DashboardViewModel dashboardViewModel,
        ISetupService setupService,
        IUserSessionService userSessionService,
        ILogger<LoginScreenViewModel> logger)
    {
        _userSessionService = userSessionService;

        _userSessionService?.UserSessions.Subscribe(x =>
        {
            if (x is not { IsAuthenticated: true }) {return;}
            
            UserName = string.Empty;
            Password = string.Empty;
            navigationService.NavigateTo(dashboardViewModel);
        });

        UserName = string.Empty;
        Password = string.Empty;

        setupService.ListOrganisations().ContinueWith(task =>
        {
            Organisations = task.Result.ToList();
            CurrentOrganisation = Organisations?.FirstOrDefault();
        });
    }

    [ObservableProperty]
    [Required(ErrorMessageResourceName = "login_error_username",ErrorMessageResourceType = typeof(Messages))]
    [MinLength(3, ErrorMessageResourceName = "login_error_username",ErrorMessageResourceType = typeof(Messages))]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string? _userName;

    [ObservableProperty] 
    [Required(ErrorMessageResourceName = "login_error_password",ErrorMessageResourceType = typeof(Messages))]
    [MinLength(8, ErrorMessageResourceName = "login_error_password",ErrorMessageResourceType = typeof(Messages))]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string? _password;

    [ObservableProperty] private IList<OrganisationData>? _organisations;

    [ObservableProperty] private OrganisationData? _currentOrganisation;

    public Interaction<UserSession?, Unit> NotifyAuthenticationResult => new();

    [RelayCommand(CanExecute = nameof(IsValid))]
    private async Task<UserSession?> Login()
    {
        var credentials = new Credentials(UserName, Password);
        var response = await _userSessionService?.Authenticate(credentials, CurrentOrganisation)!;
        //_ = await NotifyAuthenticationResult.Handle(response);
        return response;
    }

    public bool IsValid
    {
        get
        {
            ValidateProperty(UserName, nameof(UserName));
            ValidateProperty(Password, nameof(Password));
            return !HasErrors;
        }
    }

    private readonly IUserSessionService? _userSessionService;
}