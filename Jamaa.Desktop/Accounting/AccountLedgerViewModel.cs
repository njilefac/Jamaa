using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting;

public partial class AccountLedgerViewModel : ObservableObject,
    IApplicationModule,
    IRouteableViewModel,
    IRecipient<AccountLedgerNavigationRequested>,
    IDisposable
{
    [ObservableProperty] private string _accountCode = string.Empty;

    [ObservableProperty] private string _accountId = string.Empty;

    [ObservableProperty] private string _accountName = string.Empty;

    public AccountLedgerViewModel()
    {
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public Guid Id => Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    public string Title => "Account Ledger";
    public object? HeaderContent => null;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    // Operation: receives the navigation message and updates account context before the page renders.
    public void Receive(AccountLedgerNavigationRequested message)
    {
        LoadAccount(message.AccountId, message.AccountCode, message.AccountName);
    }

    // Operation: initialises the ledger view for the given account.
    public void LoadAccount(string accountId, string accountCode, string accountName)
    {
        AccountId = accountId;
        AccountCode = accountCode;
        AccountName = accountName;
    }
}