using System.Collections.ObjectModel;
using System;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Accounting.Values;

namespace Jamaa.Desktop.Accounting;

public partial class AccountItemViewModel : ObservableObject
{
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(DisplayLabel))]
    private string _code = string.Empty;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(TreeName))]
    private int _depth;

    [ObservableProperty] private string _description = string.Empty;

    [ObservableProperty] private string _id = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToggleStateLabel))]
    [NotifyPropertyChangedFor(nameof(ToggleActiveToolTip))]
    private bool _isActive = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ContraAccountDisplay))]
    private bool _isContraAccount;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(DisplayLabel))]
    private string _name = string.Empty;

    [ObservableProperty] private AccountItemViewModel? _parent;

    [ObservableProperty] private string? _parentAccountId;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(TypeDisplay))]
    private AccountType _type;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditOpeningBalance))]
    [NotifyPropertyChangedFor(nameof(CanSaveOpeningBalance))]
    private bool _isLeafAccount;

    [ObservableProperty] private string _currencySymbol = string.Empty;
    [ObservableProperty] private int _decimalPrecision = 2;
    [ObservableProperty] private string _thousandSeparator = ",";
    [ObservableProperty] private decimal _openingBalance;
    private string _openingBalanceText = "0.00";
    [ObservableProperty] private string _fiscalYearId = string.Empty;
    [ObservableProperty] private string _accountingPeriodId = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditOpeningBalance))]
    [NotifyPropertyChangedFor(nameof(CanSaveOpeningBalance))]
    private bool _isOpeningBalanceLocked;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSaveOpeningBalance))]
    private bool _isSavingOpeningBalance;

    public string DisplayLabel => $"{Code} - {Name}";
    public string TypeDisplay => Type.ToString();
    public string ContraAccountDisplay => IsContraAccount ? "Yes" : "No";

    public string TreeName => new string(' ', Depth * 2) + Name;

    public string ToggleStateLabel => IsActive ? "Active" : "Inactive";
    public string ToggleActiveToolTip => IsActive ? "Deactivate this account" : "Reactivate this account";
    public bool CanEditOpeningBalance => IsLeafAccount && !IsOpeningBalanceLocked;
    public bool CanSaveOpeningBalance => CanEditOpeningBalance && !IsSavingOpeningBalance;

    // Commands assigned by the parent ViewModel when building the tree.
    public IRelayCommand? EditCommand { get; set; }
    public IRelayCommand? ToggleActiveCommand { get; set; }
    public IRelayCommand? ViewLedgerCommand { get; set; }
    public IAsyncRelayCommand? SaveOpeningBalanceCommand { get; set; }

    public ObservableCollection<AccountItemViewModel> SubAccounts { get; } = [];

    public string OpeningBalanceText
    {
        get => _openingBalanceText;
        set
        {
            if (_openingBalanceText == value) return;

            _openingBalanceText = value;
            OnPropertyChanged();

            if (TryParseOpeningBalance(value, out var openingBalance))
            {
                OpeningBalance = openingBalance;
            }
        }
    }

    public void ForceFormatOpeningBalance()
    {
        _openingBalanceText = FormatOpeningBalanceText();
        OnPropertyChanged(nameof(OpeningBalance));
        OnPropertyChanged(nameof(OpeningBalanceText));
    }

    partial void OnOpeningBalanceChanged(decimal value)
    {
        _ = value;
        _openingBalanceText = FormatOpeningBalanceText();
        OnPropertyChanged(nameof(OpeningBalanceText));
    }

    partial void OnDecimalPrecisionChanged(int value)
    {
        _ = value;
        _openingBalanceText = FormatOpeningBalanceText();
        OnPropertyChanged(nameof(OpeningBalanceText));
    }

    partial void OnThousandSeparatorChanged(string value)
    {
        _ = value;
        _openingBalanceText = FormatOpeningBalanceText();
        OnPropertyChanged(nameof(OpeningBalanceText));
    }

    private string FormatOpeningBalanceText()
    {
        var culture = CultureInfo.CurrentCulture;
        var format = (NumberFormatInfo)culture.NumberFormat.Clone();
        format.NumberGroupSeparator = ThousandSeparator;
        return OpeningBalance.ToString($"N{DecimalPrecision}", format);
    }

    private bool TryParseOpeningBalance(string value, out decimal openingBalance)
    {
        var normalized = value.Trim()
            .Replace(" ", string.Empty)
            .Replace("'", string.Empty)
            .Replace("_", string.Empty);

        if (normalized.Length == 0)
        {
            openingBalance = 0m;
            return false;
        }

        var lastDot = normalized.LastIndexOf('.');
        var lastComma = normalized.LastIndexOf(',');

        if (lastDot >= 0 && lastComma >= 0)
        {
            var decimalSeparator = lastDot > lastComma ? '.' : ',';
            var grouped = decimalSeparator == '.'
                ? normalized.Replace(",", string.Empty)
                : normalized.Replace(".", string.Empty);
            var invariantDecimal = grouped.Replace(decimalSeparator, '.');
            return decimal.TryParse(invariantDecimal, NumberStyles.Any, CultureInfo.InvariantCulture, out openingBalance);
        }

        if (lastDot >= 0)
        {
            if (ShouldTreatSingleSeparatorAsDecimal(normalized, lastDot))
            {
                var invariantDecimal = normalized.Replace(",", string.Empty);
                return decimal.TryParse(invariantDecimal, NumberStyles.Any, CultureInfo.InvariantCulture, out openingBalance);
            }

            var grouped = normalized.Replace(".", string.Empty);
            return decimal.TryParse(grouped, NumberStyles.Any, CultureInfo.InvariantCulture, out openingBalance);
        }

        if (lastComma >= 0)
        {
            if (ShouldTreatSingleSeparatorAsDecimal(normalized, lastComma))
            {
                var invariantDecimal = normalized.Replace(",", ".");
                return decimal.TryParse(invariantDecimal, NumberStyles.Any, CultureInfo.InvariantCulture, out openingBalance);
            }

            var grouped = normalized.Replace(",", string.Empty);
            return decimal.TryParse(grouped, NumberStyles.Any, CultureInfo.InvariantCulture, out openingBalance);
        }

        if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.CurrentCulture, out openingBalance))
            return true;

        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out openingBalance);
    }

    private bool ShouldTreatSingleSeparatorAsDecimal(string value, int separatorIndex)
    {
        var digitsAfterSeparator = value.Length - separatorIndex - 1;
        return digitsAfterSeparator > 0 && digitsAfterSeparator <= (DecimalPrecision <= 0 ? 1 : DecimalPrecision);
    }
}
