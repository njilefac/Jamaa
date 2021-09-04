using EventFlow.Aggregates;
using EventFlow.EventStores;

namespace Libota.Application.Organisation.Events.Members
{
    [EventVersion("member-registration-ended", 1)]
    public class MemberRegistrationEnded : IAggregateEvent
    {
        
    }
}