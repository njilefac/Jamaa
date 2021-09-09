using EventFlow.Aggregates;
using EventFlow.EventStores;
using Libota.Application.Organisation.Aggregates;

namespace Libota.Application.Organisation.Events.Members
{
    [EventVersion("member-registration-ended", 1)]
    public class MemberRegistrationEnded : AggregateEvent<OrganisationAggregate, OrganisationId>
    {
        
    }
}