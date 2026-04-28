using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Domain.Finances.Values;

namespace Jamaa.Desktop.Accounting;

public partial class AccountItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private AccountType _type;

    [ObservableProperty]
    private AccountItemViewModel? _parent;

    public string TypeName => Type.ToString();

    public ObservableCollection<AccountItemViewModel> SubAccounts { get; } = [];
}