using EventFlow.Aggregates;
using EventFlow.EventStores;

namespace Libota.Application.Organisation.Events.Members
{
    [EventVersion("member-registered",1)]
    public class MemberRegistered : IAggregateEvent
    {
        
    }
}