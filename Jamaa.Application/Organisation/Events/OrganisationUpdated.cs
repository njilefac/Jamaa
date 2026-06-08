using Domain.Organisation.Values;
using Jamaa.Application.Shared;

namespace Jamaa.Application.Organisation.Events;

public record OrganisationUpdated(OrganisationId Id) : IHaveEntityId
{
    public string EntityId => Id.Value;
}