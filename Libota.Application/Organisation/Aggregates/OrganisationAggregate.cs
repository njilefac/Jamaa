using System;
using Domain.Entities.Members;
using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using Libota.Application.Members.Events;
using Libota.Application.Organisation.Events;

namespace Libota.Application.Organisation.Aggregates
{
    public class OrganisationAggregate : AggregateRoot<OrganisationAggregate, OrganisationId>,
        IEmit<OrganisationCreated>,
        IEmit<MemberRegistered>,
        IEmit<MemberRegistrationUpdated>,
        IEmit<MemberRegistrationEnded>
    {
        private Domain.Entities.Shared.Organisation? _state;

        public OrganisationAggregate(OrganisationId id) : base(id)
        {
            Register<OrganisationCreated>(Apply);
            Register<MemberRegistered>(Apply);
            Register<MemberRegistrationUpdated>(Apply);
            Register<MemberRegistrationEnded>(Apply);
        }

        public IExecutionResult CreateOrganisation(string name, string? description)
        {
            if (_state != null)
                return ExecutionResult.Failed("organisation already created");
            
            Emit(new OrganisationCreated(name, description));
            return ExecutionResult.Success();
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

        public void Apply(OrganisationCreated aggregateEvent)
        {
            _state = new Domain.Entities.Shared.Organisation(aggregateEvent.Name, aggregateEvent.Description);
        }
    }
}