using Domain.Finances.Values;
using Domain.Organisation.Values;
using Jamaa.Application.Shared;

namespace Jamaa.Application.Finances.Events;

public record FiscalYearUpdated(
    OrganisationId OrganisationId,
    FiscalYearId FiscalYearId,
    DateTime StartDate,
    DateTime EndDate,
    bool IsLocked) : ILibotaEvent
{
    public string EntityId => OrganisationId.Value;
}

