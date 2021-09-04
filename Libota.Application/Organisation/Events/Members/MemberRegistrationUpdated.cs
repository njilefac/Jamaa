using EventFlow.Aggregates;
using EventFlow.EventStores;

namespace Libota.Application.Organisation.Events.Members
{
    [EventVersion("member-registration-updated", 1)]
    public class MemberRegistrationUpdated : IAggregateEvent<OrganisationAggregate, OrganisationIdentity>
    {
        
    }
}