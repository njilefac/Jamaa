using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Jamaa.Application.Users.Services;
using Jamaa.Desktop.Security.Events;
using Jamaa.Desktop.Services.Updater;
using JetBrains.Annotations;
using static System.Threading.Tasks.Task;

namespace Jamaa.Desktop.Shared;

[UsedImplicitly]
public partial class MainMenuViewModel(
    IUserSessionService userSessionService,
    IApplicationUpdateService updateService)
    : ObservableObject
{
    public bool IsLoggedIn => userSessionService.CurrentUserSession?.IsAuthenticated ?? false;

    [RelayCommand(CanExecute = nameof(IsLoggedIn))]
    private Task Logout()
    {
        userSessionService.EndSession();
        WeakReferenceMessenger.Default.Send(new UserLoggedOut());
        return CompletedTask;
    }

    [RelayCommand]
    private static Task ExitApplication()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is ClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
        return CompletedTask;
    }

    [RelayCommand]
    private Task CheckForUpdates()
    {
        return updateService.CheckForUpdatesAtUserRequestAsync();
    }
}
