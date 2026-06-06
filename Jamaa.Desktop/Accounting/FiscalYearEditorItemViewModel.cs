using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;

namespace Jamaa.Desktop.Accounting;

public partial class FiscalYearEditorItemViewModel : ObservableObject
{
    private readonly ReadOnlyObservableCollection<AccountingPeriodItemViewModel> _periods;
    private readonly SourceList<AccountingPeriodItemViewModel> _periodsSource = new();

    [ObservableProperty] private DateTime _endDate;

    [ObservableProperty] private bool _isLocked;

    [ObservableProperty] private string _name = string.Empty;

    [ObservableProperty] private DateTime _startDate;

    public FiscalYearEditorItemViewModel(Guid id, DateTime startDate, DateTime endDate, bool isLocked)
    {
        _periodsSource.Connect()
            .Bind(out _periods)
            .Subscribe();

        Id = id;
        Name = BuildName(startDate, endDate);
        StartDate = startDate.Date;
        EndDate = endDate.Date;
        IsLocked = isLocked;
    }

    public Guid Id { get; }

    public ReadOnlyObservableCollection<AccountingPeriodItemViewModel> Periods => _periods;

    public string StatusLabel => IsLocked ? "Locked" : "Open";

    public string DateRangeLabel => $"{StartDate:dd MMM yyyy} — {EndDate:dd MMM yyyy}";

    public string Subtitle => $"{Periods.Count} periods • {DurationInDays} days";

    public int DurationInDays => (EndDate.Date - StartDate.Date).Days + 1;

    public void ReplacePeriods(IEnumerable<AccountingPeriodItemViewModel> periods)
    {
        _periodsSource.Edit(updater =>
        {
            updater.Clear();
            updater.AddRange(periods.OrderBy(static period => period.StartDate));
        });

        RaiseDerivedStateChanged();
    }

    public void RefreshName()
    {
        Name = BuildName(StartDate, EndDate);
    }

    partial void OnStartDateChanged(DateTime value)
    {
        RaiseDerivedStateChanged();
    }

    partial void OnEndDateChanged(DateTime value)
    {
        RaiseDerivedStateChanged();
    }

    partial void OnIsLockedChanged(bool value)
    {
        RaiseDerivedStateChanged();
    }

    private void RaiseDerivedStateChanged()
    {
        OnPropertyChanged(nameof(StatusLabel));
        OnPropertyChanged(nameof(DateRangeLabel));
        OnPropertyChanged(nameof(Subtitle));
        OnPropertyChanged(nameof(DurationInDays));
    }

    private static string BuildName(DateTime startDate, DateTime endDate)
    {
        return startDate.Year == endDate.Year
            ? $"FY {startDate:yyyy}"
            : $"FY {startDate:yyyy}/{endDate:yy}";
    }
}