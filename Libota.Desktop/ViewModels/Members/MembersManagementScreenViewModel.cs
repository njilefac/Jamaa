using System;
using Libota.Desktop.ViewModels.Shared;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.ViewModels.Members;

public class MembersManagementScreenViewModel : ReactiveObject, IRoutableViewModel, IScreen
{
    public MembersManagementScreenViewModel()
    {
        Router = new RoutingState();
    }

    public string UrlPathSegment => "members.home";
    public IScreen HostScreen { get; } = Locator.Current.GetService<MainWindowViewModel>() ?? throw new InvalidOperationException();
    public RoutingState Router { get; }
}