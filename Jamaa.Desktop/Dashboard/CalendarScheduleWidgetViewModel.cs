namespace Jamaa.Desktop.Dashboard;

public class CalendarScheduleWidgetViewModel : WidgetViewModelBase
{
    public CalendarScheduleWidgetViewModel()
    {
        Title = "Calendar Schedule";
        AllowedBoxSize = BoxSize.Wide;
        IsRemovable = false;
    }
}