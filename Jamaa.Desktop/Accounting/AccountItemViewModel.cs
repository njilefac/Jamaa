using System.Collections.ObjectModel;
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
    [ObservableProperty] private decimal _openingBalance;
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

    public void ForceFormatOpeningBalance()
    {
        OnPropertyChanged(nameof(OpeningBalance));
    }
}
