using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Jamaa.Application.Setup;
using Jamaa.Application.Users.Services;
using Jamaa.Desktop.Security.Events;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Navigation.Values;
using Jamaa.Desktop.Setup;
using JetBrains.Annotations;

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
    private readonly Dictionary<string, object?> _viewModelCache = new();
    private const string ApplicationName = "Jamaa Desktop";

    public ShellViewModel(ISetupService setupService, IUserSessionService userSessionService, IRouteResolver routeResolver)
    {
        _setupService = setupService;
        _routeResolver = routeResolver;
        _mainMenu = new MainMenuViewModel(userSessionService);

        WeakReferenceMessenger.Default.RegisterAll(this);

        DetermineInitialRoute().ContinueWith(x =>
        {
            ActiveContent = GetViewModelForRoute(x.Result);
        });

        userSessionService.UserSessions.Subscribe(x =>
        {
            ApplicationTitle = x is { IsAuthenticated: true }
                ? $"{ApplicationName} -  ({x.Organisation?.Name})"
                : ApplicationName;
        });
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
        ActiveContent = GetViewModelForRoute(Routes.Login);
    }

    public void Receive(UserLoggedOut message)
    {
        ActiveContent = GetViewModelForRoute(Routes.Login);
    }

    public void Receive(UserAuthenticated message)
    {
        ActiveContent = GetViewModelForRoute(Routes.Dashboard);
    }
    
    public void Receive(OrganisationCreated message)
    {
        ActiveContent = GetViewModelForRoute(Routes.CreateSuperUser);
    }
    
    public void Receive(SuperUserCreated message)
    {
        ActiveContent = GetViewModelForRoute(Routes.Dashboard);
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