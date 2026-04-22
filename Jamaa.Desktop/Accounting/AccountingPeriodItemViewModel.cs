using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Jamaa.Desktop.Accounting;

public partial class AccountingPeriodItemViewModel(string id, int sequenceNumber, DateTime startDate, DateTime endDate, bool isLocked) : ObservableObject
{
    public string Id { get; } = id;

    public int SequenceNumber { get; } = sequenceNumber;

    public DateTime StartDate { get; } = startDate.Date;

    public DateTime EndDate { get; } = endDate.Date;

    [ObservableProperty]
    private bool _isLocked = isLocked;

    public string Name => $"Period {SequenceNumber:00}";

    public string CoverageLabel => StartDate.Month == EndDate.Month && StartDate.Year == EndDate.Year
        ? StartDate.ToString("MMMM yyyy")
        : $"{StartDate:dd MMM} – {EndDate:dd MMM yyyy}";

    public string DateRangeLabel => $"{StartDate:dd MMM yyyy} — {EndDate:dd MMM yyyy}";

    public int DurationInDays => (EndDate - StartDate).Days + 1;

    public string DurationLabel => DurationInDays == 1 ? "1 day" : $"{DurationInDays} days";

    public string StatusLabel => IsLocked ? "Locked" : "Open";

    partial void OnIsLockedChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(StatusLabel));
    }
}

