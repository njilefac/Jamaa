using System;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Jamaa.Desktop.Dashboard;

    // Tell the JSON serializer how to handle the different widget types
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(EmptyCellViewModel), "empty")]
    [JsonDerivedType(typeof(ReportingAndAnalyticsWidgetViewModel), "reportingandanalytics")]
    [JsonDerivedType(typeof(BookkeepingWidgetViewModel), "bookkeeping")]
    [JsonDerivedType(typeof(RecentActivityFeedWidgetViewModel), "recentactivity")]
    public abstract partial class WidgetViewModelBase : ObservableObject
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();

        [ObservableProperty] private string _title = string.Empty;

        // Grid Coordinates
        [ObservableProperty] private int _row;
        [ObservableProperty] private int _column;
        [ObservableProperty] private int _rowSpan = 1;
        [ObservableProperty] private int _columnSpan = 1;

        // Resize Configuration Flags
        [ObservableProperty] private bool _isMultiColumn = false;
        [ObservableProperty] private bool _isMultiRow = false;

        public int MinColumnSpan { get; set; } = 1;
        public int MinRowSpan { get; set; } = 1;

        // Ignore these during JSON serialization to prevent circular references
        [JsonIgnore] public DashboardViewModel? ParentViewModel { get; set; }

        [JsonIgnore] public IRelayCommand<WidgetViewModelBase>? RemoveCommand { get; set; }
    }

public partial class EmptyCellViewModel : WidgetViewModelBase
{
    public EmptyCellViewModel() { Title = "Empty Slot"; }
}

public partial class ReportingAndAnalyticsWidgetViewModel : WidgetViewModelBase
{
    public ReportingAndAnalyticsWidgetViewModel() 
    { 
        Title = "Analytics"; 
        IsMultiColumn = true; 
        IsMultiRow = true; 
    }
}

public partial class BookkeepingWidgetViewModel : WidgetViewModelBase
{
    public BookkeepingWidgetViewModel() 
    { 
        Title = "Financial Summary"; 
        IsMultiColumn = true; 
    }
}

public partial class RecentActivityFeedWidgetViewModel : WidgetViewModelBase
{
    public RecentActivityFeedWidgetViewModel()
    {
        Title = "Recent Activity";
        IsMultiRow = true;
    }
}