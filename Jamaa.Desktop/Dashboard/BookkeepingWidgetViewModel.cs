namespace Jamaa.Desktop.Dashboard;

public partial class BookkeepingWidgetViewModel : WidgetViewModelBase
{
    public BookkeepingWidgetViewModel() 
    { 
        Title = "Financial Summary"; 
        AllowedBoxSize = BoxSize.Wide;
        IsRemovable = false;
    }
}