using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Security.Values;
using Libota.Application.Setup;
using Libota.Application.Users;
using Libota.Application.Users.Services;
using Libota.Data.Models.Organisation;
using Libota.Desktop.Navigation;
using Libota.Desktop.ViewModels.Shared;
using Microsoft.Extensions.Logging;

namespace Libota.Desktop.ViewModels.Security;

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
            if (x is not { IsAuthenticated: true }) return;
            navigationService.NavigateTo(dashboardViewModel!);
        });

        UserName = string.Empty;
        Password = string.Empty;

        setupService.ListOrganisations().ContinueWith(task =>
        {
            Organisations = task.Result.ToList();
            CurrentOrganisation = Organisations?.FirstOrDefault();
        });

        // this.ValidationRule(vm => vm.UserName,
        //     x => !string.IsNullOrEmpty(x) && x.Length >= 3,
        //     string.Format(Messages.login_error_username, 3));
        //
        // this.ValidationRule(vm => vm.Password,
        //     x => !string.IsNullOrEmpty(x) && x.Length >= 6,
        //     string.Format(Messages.login_error_password, 6));

        //NotifyAuthenticationResult = new Interaction<UserSession?, Unit>();

        // Login = ReactiveCommand.CreateFromTask(AuthenticateUser, this.IsValid());

        //Login.ThrownExceptions.Subscribe(ex => { logger.LogError(ex, "login error {Exception}", ex.Message); });
    }

    [ObservableProperty] private string? _userName;

    [ObservableProperty] private string? _password;

    [ObservableProperty] private IList<OrganisationData>? _organisations;

    [ObservableProperty] private OrganisationData? _currentOrganisation;

    //public Interaction<UserSession?, Unit> NotifyAuthenticationResult { get; set; }

    public string UrlPathSegment => "login";

    [RelayCommand]
    private async Task<UserSession?> AuthenticateUser()
    {
        var credentials = new Credentials(UserName, Password);
        var response = await _userSessionService?.Authenticate(credentials, CurrentOrganisation)!;
        return response;
    }

    private readonly IUserSessionService? _userSessionService;
}