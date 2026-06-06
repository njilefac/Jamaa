using Domain.Accounting.Values;
using Domain.Organisation.Values;
using Jamaa.Application.Shared;

namespace Jamaa.Application.Accounting.Events;

public record FiscalYearPeriodsRegenerated(
    OrganisationId OrganisationId,
    FiscalYearId FiscalYearId,
    IReadOnlyList<string> DeletedPeriodIds,
    IReadOnlyList<AccountingPeriodInfo> CreatedPeriods) : IJamaaEvent
{
    public string EntityId => OrganisationId.Value;
}

public record AccountingPeriodInfo(
    string Id,
    int SequenceNumber,
    DateTime StartDate,
    DateTime EndDate,
    bool IsLocked);