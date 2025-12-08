using CommunityToolkit.Mvvm.ComponentModel;
using Libota.Desktop.Navigation;

namespace Libota.Desktop.ViewModels.Members;

public partial class MembersManagementScreenViewModel: ObservableObject
{
    public MembersManagementScreenViewModel(INavigationService navigationService, MembersOverviewPageViewModel membersOverviewPageViewModel)
    {
        _navigationService = navigationService;
        CurrentContent = membersOverviewPageViewModel;
    }
    
    [ObservableProperty]
    private ObservableObject? _currentContent;

    private readonly INavigationService _navigationService;
}