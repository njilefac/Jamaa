using Domain.Entities.Members;
using EventFlow.Aggregates;
using Libota.Application.Members.Events;

namespace Libota.Application.Members.Aggregates
{
    public class MemberAggregate : AggregateRoot<MemberAggregate, MemberId>,
        IEmit<MemberRegistered>,
        IEmit<MemberRegistrationUpdated>,
        IEmit<MemberContactDetailsChanged>
    {
        private Member _state;

        public MemberAggregate(MemberId id) : base(id)
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