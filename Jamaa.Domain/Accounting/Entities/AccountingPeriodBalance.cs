using Domain.Accounting.Values;
using Domain.Organisation.Values;

namespace Domain.Accounting.Entities;

/// <summary>
/// Represents the balance of an account for a specific accounting period and fiscal year.
/// </summary>
public sealed record AccountingPeriodBalance(
    AccountingPeriodBalanceId Id,
    AccountId AccountId,
    FiscalYearId FiscalYearId,
    AccountingPeriodId AccountingPeriodId,
    OrganisationId OrganisationId,
    MoneyAmount OpeningBalance,
    MoneyAmount ClosingBalance);
