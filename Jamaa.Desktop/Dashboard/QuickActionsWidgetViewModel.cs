using System.Text.Json.Serialization;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Jamaa.Desktop.Dashboard;

public class QuickActionsWidgetViewModel : WidgetViewModelBase
{
    public QuickActionsWidgetViewModel()
    {
        Title = "Quick Actions";
        AllowedBoxSize = BoxSize.Small;
        IsRemovable = true;

        RegisterMemberCommand = new RelayCommand(() =>
        {
            /* TODO: Implement member registration navigation */
        });
        ReceiveMoneyCommand = new RelayCommand(() =>
        {
            /* TODO: Implement receive money navigation */
        });
        SpendMoneyCommand = new RelayCommand(() =>
        {
            /* TODO: Implement spend money navigation */
        });
    }

    [JsonIgnore] public ICommand RegisterMemberCommand { get; }
    [JsonIgnore] public ICommand ReceiveMoneyCommand { get; }
    [JsonIgnore] public ICommand SpendMoneyCommand { get; }
}