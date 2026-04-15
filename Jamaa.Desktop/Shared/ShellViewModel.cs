using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Jamaa.Application.Setup;
using Jamaa.Application.Users.Services;
using Jamaa.Desktop.Dashboard;
using Jamaa.Desktop.Security.Events;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Navigation.Values;
using Jamaa.Desktop.Setup;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Avalonia.Threading;

namespace Jamaa.Desktop.Shared;

[UsedImplicitly]
public partial class ShellViewModel : ObservableObject,
    IRecipient<OrganisationCreated>,
    IRecipient<SuperUserCreated>,
    IRecipient<UserAuthenticated>,
    IRecipient<AuthenticationFailed>,
    IRecipient<UserLoggedOut>, IDisposable
{
    private readonly ISetupService _setupService;
    private readonly IRouteResolver _routeResolver;
    private readonly ILogger<ShellViewModel> _logger;
    private readonly Dictionary<string, object?> _viewModelCache = new();
    private const string ApplicationName = "Jamaa Desktop";

    public ShellViewModel(ISetupService setupService, IUserSessionService userSessionService,
        IRouteResolver routeResolver, DashboardViewModel dashboardViewModel, ILogger<ShellViewModel> logger)
    {
        _setupService = setupService;
        _routeResolver = routeResolver;
        _logger = logger;
        _mainMenu = new MainMenuViewModel(userSessionService);

        WeakReferenceMessenger.Default.RegisterAll(this);

        _ = InitializeAsync();

        userSessionService.UserSessions.Subscribe(x =>
        {
            ApplicationTitle = x is { IsAuthenticated: true }
                ? $"{ApplicationName} -  ({x.Organisation?.Name})"
                : ApplicationName;
        });
    }

    private async Task InitializeAsync()
    {
        try
        {
            var route = await DetermineInitialRoute();
            Dispatcher.UIThread.Post(() => { ActiveContent = GetViewModelForRoute(route); });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to determine initial route");
        }
    }

    [ObservableProperty] private string? _applicationTitle = ApplicationName;
    [ObservableProperty] private ObservableObject _mainMenu;
    [ObservableProperty] private object? _activeContent;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    public void Receive(AuthenticationFailed message)
    {
        Dispatcher.UIThread.Post(() => { ActiveContent = GetViewModelForRoute(Routes.Login); });
    }

    public void Receive(UserLoggedOut message)
    {
        Dispatcher.UIThread.Post(() => { ActiveContent = GetViewModelForRoute(Routes.Login); });
    }

    public void Receive(UserAuthenticated message)
    {
        Dispatcher.UIThread.Post(() => { ActiveContent = GetViewModelForRoute(Routes.Home); });
    }

    public void Receive(OrganisationCreated message)
    {
        Dispatcher.UIThread.Post(() => { ActiveContent = GetViewModelForRoute(Routes.CreateSuperUser); });
    }

    public void Receive(SuperUserCreated message)
    {
        Dispatcher.UIThread.Post(() => { ActiveContent = GetViewModelForRoute(Routes.Home); });
    }
    
    private object? GetViewModelForRoute(string path )
    {
        if(_viewModelCache.TryGetValue(path, out var cachedVm))
        {
            return cachedVm;
        }
        
        var vm = _routeResolver.Resolve(path);
        _viewModelCache[path] = vm;
        return vm;
    }
    
    private async Task<string> DetermineInitialRoute()
    {
        var existingOrganisations = await _setupService.ListOrganisations();
        if (!existingOrganisations.Any())
        {
            return Routes.CreateOrganisation;
        }

        var superUser = await _setupService.GetSuperUser();
        return superUser == null ? Routes.CreateSuperUser : Routes.Login;
    }
}