using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using JetBrains.Annotations;
using Libota.Application.Users.Services;
using Libota.Desktop.Security;
using static System.Threading.Tasks.Task;

namespace Libota.Desktop.Shared;

[UsedImplicitly]
public partial class MainMenuViewModel(IUserSessionService userSessionService)
    : ObservableObject
{
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

    public bool IsLoggedIn => userSessionService.CurrentUserSession?.IsAuthenticated ?? false;
}