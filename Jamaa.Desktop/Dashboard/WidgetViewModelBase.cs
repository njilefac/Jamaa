using System;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Jamaa.Desktop.Dashboard;

    public enum BoxSize
    {
        Small,
        Wide
    }

    // Tell the JSON serializer how to handle the different widget types
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(EmptyCellViewModel), "empty")]
    [JsonDerivedType(typeof(ReportingAndAnalyticsWidgetViewModel), "reportingandanalytics")]
    [JsonDerivedType(typeof(BookkeepingWidgetViewModel), "bookkeeping")]
    [JsonDerivedType(typeof(RecentActivityFeedWidgetViewModel), "recentactivity")]
    [JsonDerivedType(typeof(CalendarScheduleWidgetViewModel), "calendarschedule")]
    public abstract partial class WidgetViewModelBase : ObservableObject
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();

        [ObservableProperty] private string _title = string.Empty;

        // Grid Coordinates
        [ObservableProperty] private int _row;
        [ObservableProperty] private int _column;
        [ObservableProperty] private int _rowSpan = 1;
        [ObservableProperty] private int _columnSpan = 1;

        // Bento Box Configuration
        [ObservableProperty] private BoxSize _allowedBoxSize = BoxSize.Small;

        // Ignore these during JSON serialization to prevent circular references
        [JsonIgnore] public DashboardViewModel? ParentViewModel { get; set; }

        [JsonIgnore] public IRelayCommand<WidgetViewModelBase>? RemoveCommand { get; set; }
    }

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

public partial class ReportingAndAnalyticsWidgetViewModel : WidgetViewModelBase
{
    public ReportingAndAnalyticsWidgetViewModel() 
    { 
        Title = "Analytics"; 
        AllowedBoxSize = BoxSize.Wide;
    }
}

public partial class BookkeepingWidgetViewModel : WidgetViewModelBase
{
    public BookkeepingWidgetViewModel() 
    { 
        Title = "Financial Summary"; 
        AllowedBoxSize = BoxSize.Wide;
    }
}

public partial class RecentActivityFeedWidgetViewModel : WidgetViewModelBase
{
    public RecentActivityFeedWidgetViewModel()
    {
        Title = "Recent Activity";
        AllowedBoxSize = BoxSize.Small;
    }
}

public partial class CalendarScheduleWidgetViewModel : WidgetViewModelBase
{
    public CalendarScheduleWidgetViewModel()
    {
        Title = "Calendar Schedule";
        AllowedBoxSize = BoxSize.Wide;
    }
}