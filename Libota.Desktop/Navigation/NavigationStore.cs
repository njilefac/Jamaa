using CommunityToolkit.Mvvm.ComponentModel;

namespace Libota.Desktop.Navigation;

public partial class NavigationStore: ObservableObject
{
    [ObservableProperty]
    private ObservableObject? _currentViewModel;
}