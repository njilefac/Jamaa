using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Domain.Security.Values;
using JetBrains.Annotations;
using Libota.Application.Setup;
using Libota.Application.Users;
using Libota.Application.Users.Services;
using Libota.Data.Models.Organisation;
using Libota.Desktop.Assets.Resources;
using Libota.Desktop.Security.Events;
using Libota.Desktop.Services.Notifications;
using Microsoft.Extensions.Logging;

namespace Libota.Desktop.Security;

[UsedImplicitly]
public partial class LoginScreenViewModel : ObservableValidator
{
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

    [RelayCommand(CanExecute = nameof(IsValid))]
    private async Task<UserSession?> Login()
    {
        var credentials = new Credentials(UserName, Password);
        var response = await _userSessionService?.Authenticate(credentials, CurrentOrganisation)!;
        if (response is null or { IsAuthenticated: false })
        {
            _notificationService.Show(Messages.login_authentication_failed_title, Messages.login_authentication_failed_message, NotificationType.Error);
        }
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
    private readonly INotificationService _notificationService;
}