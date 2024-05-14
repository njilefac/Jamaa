using Domain.Organisation.Values;
using Libota.Application.Shared;

namespace Libota.Application.Organisation.Events
{
    public record OrganisationCreated(OrganisationId Id, string Name, string? Description) : ILibotaEvent
    {
        public string EntityId => Id.Value;
    }
}