using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jamaa.Desktop.Assets.Resources;

namespace Jamaa.Desktop.Dashboard;

// Tell the JSON serializer how to handle the different widget types
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(EmptyCellViewModel), "empty")]
    [JsonDerivedType(typeof(ReportingAndAnalyticsWidgetViewModel), "reportingandanalytics")]
    [JsonDerivedType(typeof(BookkeepingWidgetViewModel), "bookkeeping")]
    [JsonDerivedType(typeof(RecentActivityFeedWidgetViewModel), "recentactivity")]
    [JsonDerivedType(typeof(CalendarScheduleWidgetViewModel), "calendarschedule")]
    [JsonDerivedType(typeof(AlertsAndNotificationsWidgetViewModel), "alerts")]
    [JsonDerivedType(typeof(QuickActionsWidgetViewModel), "quickactions")]
    [JsonDerivedType(typeof(MembershipStatsWidgetViewModel), "membershipstats")]
    public abstract partial class WidgetViewModelBase : ObservableObject
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();

        [ObservableProperty] private string _title = string.Empty;

        // Grid Coordinates
        [ObservableProperty] private int _row;
        [ObservableProperty] private int _column;

        // Bento Box Configuration
        [ObservableProperty] private BoxSize _allowedBoxSize = BoxSize.Small;
        [ObservableProperty] private bool _isRemovable = true;

        // Ignore these during JSON serialization to prevent circular references
        [JsonIgnore] public DashboardViewModel? ParentViewModel { get; set; }

        [JsonIgnore] public IRelayCommand<WidgetViewModelBase>? RemoveCommand { get; set; }
        [JsonIgnore] public IEnumerable<WidgetViewModelBase> CompatibleWidgets => ParentViewModel?.GetCompatibleWidgets(AllowedBoxSize) ?? Enumerable.Empty<WidgetViewModelBase>();

        [JsonIgnore] public bool HasCompatibleWidgets => CompatibleWidgets.Any();

        [JsonIgnore]
        public string FlyoutTitle => HasCompatibleWidgets
            ? Messages.dashboard_available_widgets
            : Messages.dashboard_no_widgets_available;

        public void NotifyCompatibilityChanged()
        {
            OnPropertyChanged(nameof(CompatibleWidgets));
            OnPropertyChanged(nameof(HasCompatibleWidgets));
            OnPropertyChanged(nameof(FlyoutTitle));
        }
    }