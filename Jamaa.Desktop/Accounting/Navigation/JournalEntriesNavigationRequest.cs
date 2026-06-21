namespace Jamaa.Desktop.Accounting.Navigation;

/// <summary>
///     Operation: Carries navigation filter intent into the Journal Entries workspace.
/// </summary>
public sealed record JournalEntriesNavigationRequest(
    bool ExpenseAccountsOnly,
    string? AccountId = null,
    string? AccountCode = null,
    string? AccountName = null)
{
    public bool HasAccountContext => !string.IsNullOrWhiteSpace(AccountId);

    public static JournalEntriesNavigationRequest AllAccounts()
    {
        return new JournalEntriesNavigationRequest(false);
    }

    public static JournalEntriesNavigationRequest OnlyExpenseAccounts()
    {
        return new JournalEntriesNavigationRequest(true);
    }

    public static JournalEntriesNavigationRequest ForAccount(string accountId, string accountCode, string accountName)
    {
        return new JournalEntriesNavigationRequest(false, accountId, accountCode, accountName);
    }
}