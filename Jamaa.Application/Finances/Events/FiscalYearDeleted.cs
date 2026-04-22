using Domain.Finances.Values;
using Domain.Organisation.Values;
using Jamaa.Application.Shared;

namespace Jamaa.Application.Finances.Events;

public record FiscalYearDeleted(OrganisationId OrganisationId, FiscalYearId FiscalYearId) : ILibotaEvent
{
    public string EntityId => OrganisationId.Value;
}

