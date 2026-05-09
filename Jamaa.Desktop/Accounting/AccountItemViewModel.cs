using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Finances.Values;

namespace Jamaa.Desktop.Accounting;

public partial class AccountItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayLabel))]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayLabel))]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TypeDisplay))]
    private AccountType _type;

    [ObservableProperty]
    private string? _parentAccountId;

    [ObservableProperty]
    private AccountItemViewModel? _parent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TreeName))]
    private int _depth;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToggleStateLabel))]
    [NotifyPropertyChangedFor(nameof(ToggleActiveToolTip))]
    private bool _isActive = true;

    public string DisplayLabel => $"{Code} - {Name}";
    public string TypeDisplay => Type.ToString();

    public string TreeName => new string(' ', Depth * 2) + Name;

    public string ToggleStateLabel => IsActive ? "Active" : "Inactive";
    public string ToggleActiveToolTip => IsActive ? "Deactivate this account" : "Reactivate this account";

    // Commands assigned by the parent ViewModel when building the tree.
    public IRelayCommand? EditCommand { get; set; }
    public IRelayCommand? ToggleActiveCommand { get; set; }
    public IRelayCommand? ViewLedgerCommand { get; set; }

    public ObservableCollection<AccountItemViewModel> SubAccounts { get; } = [];
}