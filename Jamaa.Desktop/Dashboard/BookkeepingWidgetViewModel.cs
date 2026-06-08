namespace Jamaa.Desktop.Dashboard;

public class BookkeepingWidgetViewModel : WidgetViewModelBase
{
    public BookkeepingWidgetViewModel()
    {
        Title = "Financial Summary";
        AllowedBoxSize = BoxSize.Wide;
        IsRemovable = false;
    }
}