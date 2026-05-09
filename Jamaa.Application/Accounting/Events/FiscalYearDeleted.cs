using Domain.Accounting.Values;
using Domain.Organisation.Values;
using Jamaa.Application.Shared;

namespace Jamaa.Application.Accounting.Events;

public record FiscalYearDeleted(OrganisationId OrganisationId, FiscalYearId FiscalYearId) : IJamaaEvent
{
    public string EntityId => OrganisationId.Value;
}