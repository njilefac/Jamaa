using System;
using System.Collections.Generic;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting;

public partial class AccountingCurrencyAndDateFormatsViewModel : ObservableObject, IApplicationModule, IRouteableViewModel
{
    public Guid Id => Guid.Parse("d3a693eb-5b4c-44ee-ae45-cf32b5ec4fe9");
    public string Title => "Currency & Date Formats";
    public object? HeaderContent => null;

    public IReadOnlyList<string> BaseCurrencyOptions { get; } = ["USD", "KES", "EUR", "GBP", "ZAR", "NGN"];
    public IReadOnlyList<string> DateFormatOptions { get; } = ["DD/MM/YYYY", "MM/DD/YYYY", "YYYY-MM-DD"];
    public IReadOnlyList<int> DecimalPrecisionOptions { get; } = [0, 1, 2, 3, 4];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattingPreview))]
    private string _selectedBaseCurrency = "USD";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattingPreview))]
    private string _selectedDateFormat = "DD/MM/YYYY";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattingPreview))]
    private int _selectedDecimalPrecision = 2;

    public string FormattingPreview
    {
        get
        {
            var exampleDate = new DateTime(2026, 4, 24).ToString(ResolveDotNetDateFormat(SelectedDateFormat), CultureInfo.InvariantCulture);
            var exampleAmount = 1234567.0m.ToString($"N{SelectedDecimalPrecision}", CultureInfo.InvariantCulture);
            return $"{SelectedBaseCurrency} {exampleAmount}   |   {exampleDate}";
        }
    }

    // Operation: maps the selected UI date format to a .NET format token for preview rendering.
    private static string ResolveDotNetDateFormat(string selectedDateFormat)
    {
        return selectedDateFormat switch
        {
            "DD/MM/YYYY" => "dd/MM/yyyy",
            "MM/DD/YYYY" => "MM/dd/yyyy",
            "YYYY-MM-DD" => "yyyy-MM-dd",
            _ => "dd/MM/yyyy"
        };
    }
}

