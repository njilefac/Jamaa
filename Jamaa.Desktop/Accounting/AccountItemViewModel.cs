using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
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

    public string DisplayLabel => $"{Code} - {Name}";
    public string TypeDisplay => Type.ToString();

    public string TreeName => new string(' ', Depth * 2) + Name;

    public ObservableCollection<AccountItemViewModel> SubAccounts { get; } = [];
}