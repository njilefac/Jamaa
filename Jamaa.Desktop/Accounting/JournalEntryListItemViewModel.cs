namespace Jamaa.Desktop.Accounting;

/// <summary>
/// Operation: Represents a single journal-entry row for list rendering.
/// </summary>
public sealed record JournalEntryListItemViewModel(
    string Date,
    string Reference,
    string Account,
    string AccountCategory,
    decimal Amount,
    bool IsExpenseAccount);

