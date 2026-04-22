using Domain.Finances.Values;
using Domain.Organisation.Values;
using Jamaa.Application.Shared;

namespace Jamaa.Application.Finances.Events;

public record AccountingPeriodDeleted(
    OrganisationId OrganisationId,
    FiscalYearId FiscalYearId,
    AccountingPeriodId AccountingPeriodId) : ILibotaEvent
{
    public string EntityId => OrganisationId.Value;
}

