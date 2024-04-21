using Akka.Actor;
using Domain.Members;
using Libota.Application.Members.Events;

namespace Libota.Application.Members.Aggregates
{
    public class MemberAggregate : ReceiveActor
    {
        private Member _state;

        public MemberAggregate(MemberId id)
        {
        }

        public void Apply(MemberRegistered aggregateEvent)
        {
            throw new System.NotImplementedException();
        }

        public void Apply(MemberRegistrationUpdated aggregateEvent)
        {
            throw new System.NotImplementedException();
        }

        public void Apply(MemberContactDetailsChanged aggregateEvent)
        {
            throw new System.NotImplementedException();
        }
    }
}