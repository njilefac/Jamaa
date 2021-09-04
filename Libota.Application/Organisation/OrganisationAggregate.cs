using System;
using Domain.Entities.Members;
using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using Libota.Application.Organisation.Events.Members;

namespace Libota.Application.Organisation
{
    public class OrganisationAggregate : AggregateRoot<OrganisationAggregate, OrganisationIdentity>,
        IEmit<MemberRegistered>,
        IEmit<MemberRegistrationUpdated>,
        IEmit<MemberRegistrationEnded>
    {
        private Domain.Entities.Shared.Organisation _state;

        public OrganisationAggregate(OrganisationIdentity id) : base(id)
        {
        }

        public IExecutionResult RegisterNewMember(Member member)
        {
            throw new NotImplementedException();
        }

        public void Apply(MemberRegistered aggregateEvent)
        {
            throw new NotImplementedException();
        }

        public void Apply(MemberRegistrationUpdated aggregateEvent)
        {
            throw new NotImplementedException();
        }

        public void Apply(MemberRegistrationEnded aggregateEvent)
        {
            throw new NotImplementedException();
        }
    }
}