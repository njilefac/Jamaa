namespace Jamaa.Desktop.Dashboard;

public partial class EmptyCellViewModel : WidgetViewModelBase
{
    public EmptyCellViewModel() { Title = string.Empty; }
    public EmptyCellViewModel(int row, int column, BoxSize size)
    {
        Title = string.Empty;
        Row = row;
        Column = column;
        AllowedBoxSize = size;
    }
}