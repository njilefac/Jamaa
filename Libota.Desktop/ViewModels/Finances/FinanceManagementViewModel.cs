using System;
using Libota.Application.Users.Services;
using Libota.Desktop.ViewModels.Shared;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.ViewModels.Finances;

public class FinanceManagementViewModel : ReactiveObject, IRoutableViewModel
{
    public FinanceManagementViewModel()
    {
        HostScreen = Locator.Current.GetService<MainWindowViewModel>() ?? throw new InvalidOperationException();
        _userSessionService = Locator.Current.GetService<IUserSessionService>() ?? throw new InvalidOperationException();
    }
    
    public string? UrlPathSegment => "finance";
    public IScreen HostScreen { get; }
    
    private readonly IUserSessionService _userSessionService;
}