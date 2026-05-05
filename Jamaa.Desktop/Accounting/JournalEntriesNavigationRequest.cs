namespace Jamaa.Desktop.Accounting;

/// <summary>
/// Operation: Carries navigation filter intent into the Journal Entries workspace.
/// </summary>
public sealed record JournalEntriesNavigationRequest(
    bool ExpenseAccountsOnly,
    string? AccountId = null,
    string? AccountCode = null,
    string? AccountName = null)
{
    public bool HasAccountContext => !string.IsNullOrWhiteSpace(AccountId);

    public static JournalEntriesNavigationRequest AllAccounts() => new(false);

    public static JournalEntriesNavigationRequest OnlyExpenseAccounts() => new(true);

    public static JournalEntriesNavigationRequest ForAccount(string accountId, string accountCode, string accountName) =>
        new(false, accountId, accountCode, accountName);
}


