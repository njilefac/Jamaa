using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Jamaa.Desktop.Accounting;

/// <summary>
///     Operation: Represents a clickable metric card on the GL Dashboard that navigates to a sub-module.
/// </summary>
public sealed class DashboardMetricCardViewModel : ObservableObject
{
    public DashboardMetricCardViewModel(string metricValue, string metricLabel, string description,
        IRelayCommand? navigateCommand = null)
    {
        MetricValue = metricValue;
        MetricLabel = metricLabel;
        Description = description;
        NavigateCommand = navigateCommand ?? new RelayCommand(static () => { });
    }

    public string MetricValue { get; }
    public string MetricLabel { get; }
    public string Description { get; }
    public IRelayCommand NavigateCommand { get; }
}