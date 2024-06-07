using System;
using Libota.Application.Users.Services;
using Libota.Desktop.ViewModels.Shared;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.ViewModels.Groups;

public class GroupManagementViewModel: ReactiveObject, IRoutableViewModel
{
    private readonly IUserSessionService _userSessionService;

    public GroupManagementViewModel()
    {
        _userSessionService = Locator.Current.GetService<IUserSessionService>() ?? throw new InvalidOperationException();
        HostScreen = Locator.Current.GetService<MainWindowViewModel>() ?? throw new InvalidOperationException();;
    }

    public string? UrlPathSegment => "groups";
    public IScreen HostScreen { get; }
}