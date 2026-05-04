using Domain.Organisation.Values;
using Jamaa.Application.Shared;

namespace Jamaa.Application.Organisation.Events
{
    public record OrganisationCreated(OrganisationId Id, string Name, string? Description) : IJamaaEvent
    {
        public string EntityId => Id.Value;
    }
}