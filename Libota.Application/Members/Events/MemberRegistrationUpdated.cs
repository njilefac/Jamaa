using EventFlow.Aggregates;
using EventFlow.EventStores;
using Libota.Application.Organisation.Aggregates;

namespace Libota.Application.Members.Events
{
    [EventVersion("member-registration-updated", 1)]
    public class MemberRegistrationUpdated : IAggregateEvent<OrganisationAggregate, OrganisationId>
    {
    }
}