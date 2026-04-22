using Domain.Finances.Values;
using Domain.Organisation.Values;
using Jamaa.Application.Shared;

namespace Jamaa.Application.Finances.Events;

public record AccountingPeriodUpdated(
    OrganisationId OrganisationId,
    FiscalYearId FiscalYearId,
    AccountingPeriodId AccountingPeriodId,
    int SequenceNumber,
    DateTime StartDate,
    DateTime EndDate,
    bool IsLocked) : ILibotaEvent
{
    public string EntityId => OrganisationId.Value;
}

