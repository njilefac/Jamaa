using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Domain.Security.Values;
using Jamaa.Application.Setup;
using Jamaa.Application.Users;
using Jamaa.Application.Users.Services;
using Jamaa.Data.Models.Organisation;
using Jamaa.Desktop.Assets.Resources;
using Jamaa.Desktop.Security.Events;
using Jamaa.Desktop.Services.Notifications;
using Jamaa.Desktop.Shared;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Jamaa.Desktop.Security;

[UsedImplicitly]
public partial class LoginScreenViewModel : ValidatableFormViewModel
{
    private readonly INotificationService _notificationService;

    private readonly IUserSessionService? _userSessionService;

    [ObservableProperty] private OrganisationData? _currentOrganisation;

    [ObservableProperty] private IList<OrganisationData>? _organisations;

    [ObservableProperty]
    [Required(ErrorMessageResourceName = "login_error_password", ErrorMessageResourceType = typeof(Messages))]
    [MinLength(8, ErrorMessageResourceName = "login_error_password", ErrorMessageResourceType = typeof(Messages))]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string? _password;

    [ObservableProperty]
    [Required(ErrorMessageResourceName = "login_error_username", ErrorMessageResourceType = typeof(Messages))]
    [MinLength(3, ErrorMessageResourceName = "login_error_username", ErrorMessageResourceType = typeof(Messages))]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string? _userName;

    public LoginScreenViewModel(
        ISetupService setupService,
        IUserSessionService userSessionService,
        ILogger<LoginScreenViewModel> logger,
        INotificationService notificationService)
    {
        _userSessionService = userSessionService;
        _notificationService = notificationService;

        _userSessionService?.UserSessions.Subscribe(session =>
        {
            if (session is not { IsAuthenticated: true })
            {
                WeakReferenceMessenger.Default.Send(new AuthenticationFailed());
                return;
            }

            WeakReferenceMessenger.Default.Send(new UserAuthenticated(session));
            UserName = string.Empty;
            Password = string.Empty;
        });

        setupService.ListOrganisations().ContinueWith(task =>
        {
            Organisations = task.Result.ToList();
            CurrentOrganisation = Organisations.Count == 1 ? Organisations?.Single() : null;
        });
    }

    public bool CanLogin
    {
        get
        {
            ValidateProperty(UserName, nameof(UserName));
            ValidateProperty(Password, nameof(Password));
            return !HasErrors;
        }
    }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task<UserSession?> Login()
    {
        var credentials = new Credentials(UserName, Password);
        var response = await _userSessionService?.Authenticate(credentials, CurrentOrganisation)!;
        if (response is null or { IsAuthenticated: false })
            _notificationService.Show(Messages.login_authentication_failed_title,
                Messages.login_authentication_failed_message, NotificationType.Error);
        ResetValidationState();
        return response;
    }
}