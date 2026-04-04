namespace Jamaa.Desktop.Dashboard;

public partial class ReportingAndAnalyticsWidgetViewModel : WidgetViewModelBase
{
    public ReportingAndAnalyticsWidgetViewModel() 
    { 
        Title = "Analytics"; 
        AllowedBoxSize = BoxSize.Wide;
        IsRemovable = false;
    }
}