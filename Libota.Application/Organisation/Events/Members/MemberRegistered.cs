using EventFlow.Aggregates;
using EventFlow.EventStores;
using Libota.Application.Organisation.Aggregates;

namespace Libota.Application.Organisation.Events.Members
{
    [EventVersion("member-registered",1)]
    public class MemberRegistered : AggregateEvent<OrganisationAggregate, OrganisationId>
    {
        
    }
}