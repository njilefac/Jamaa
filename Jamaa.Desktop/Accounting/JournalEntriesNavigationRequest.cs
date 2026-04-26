namespace Jamaa.Desktop.Accounting;

/// <summary>
/// Operation: Carries navigation filter intent into the Journal Entries workspace.
/// </summary>
public sealed record JournalEntriesNavigationRequest(bool ExpenseAccountsOnly)
{
    public static JournalEntriesNavigationRequest AllAccounts() => new(false);

    public static JournalEntriesNavigationRequest OnlyExpenseAccounts() => new(true);
}


