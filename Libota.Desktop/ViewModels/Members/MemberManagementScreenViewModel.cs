using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using Libota.Desktop.Navigation;

namespace Libota.Desktop.ViewModels.Members;

[UsedImplicitly]
public partial class MemberManagementScreenViewModel : ObservableObject
{
    public MemberManagementScreenViewModel(IRouteResolver routeResolver)
    {
        var content = routeResolver.Resolve(Routes.MembersOverview);
        ActiveContent = "member overview";
    }

    [ObservableProperty] private object? _activeContent;
}