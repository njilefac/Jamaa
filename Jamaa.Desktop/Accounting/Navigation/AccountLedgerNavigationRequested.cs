namespace Jamaa.Desktop.Accounting.Navigation;

/// <summary>
///     Carries the account context needed to display a ledger page.
/// </summary>
public record AccountLedgerNavigationRequested(string AccountId, string AccountCode, string AccountName);