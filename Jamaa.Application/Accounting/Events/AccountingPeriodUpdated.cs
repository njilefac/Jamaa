using Domain.Accounting.Values;
using Domain.Organisation.Values;
using Jamaa.Application.Shared;

namespace Jamaa.Application.Accounting.Events;

public record AccountingPeriodUpdated(
    OrganisationId OrganisationId,
    FiscalYearId FiscalYearId,
    AccountingPeriodId AccountingPeriodId,
    int SequenceNumber,
    DateTime StartDate,
    DateTime EndDate,
    bool IsLocked) : IJamaaEvent
{
    public string EntityId => OrganisationId.Value;
}