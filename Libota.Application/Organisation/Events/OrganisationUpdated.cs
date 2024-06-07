using Domain.Organisation.Values;
using Libota.Application.Shared;

namespace Libota.Application.Organisation.Events
{
    public record OrganisationUpdated(OrganisationId Id) : IHaveEntityId
    {
        public string EntityId => Id.Value;
    }
}