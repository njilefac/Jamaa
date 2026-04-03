using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace Jamaa.Desktop.Dashboard;

public partial class QuickActionsWidgetViewModel : WidgetViewModelBase
{
    public ICommand RegisterMemberCommand { get; }
    public ICommand ReceiveMoneyCommand { get; }
    public ICommand SpendMoneyCommand { get; }

    public QuickActionsWidgetViewModel()
    {
        Title = "Quick Actions";
        AllowedBoxSize = BoxSize.Small;
        IsRemovable = true;

        RegisterMemberCommand = new RelayCommand(() => { /* TODO: Implement member registration navigation */ });
        ReceiveMoneyCommand = new RelayCommand(() => { /* TODO: Implement receive money navigation */ });
        SpendMoneyCommand = new RelayCommand(() => { /* TODO: Implement spend money navigation */ });
    }
}
