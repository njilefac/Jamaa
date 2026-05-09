namespace Jamaa.Desktop.Dashboard;

public class AlertsAndNotificationsWidgetViewModel : WidgetViewModelBase
{
    public AlertsAndNotificationsWidgetViewModel()
    {
        Title = "Alerts & Notifications";
        AllowedBoxSize = BoxSize.Small;
        IsRemovable = true;
    }
}