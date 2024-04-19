using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Members;
using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Queries;
using Libota.Application.Members.Events;
using Libota.Application.Organisation.Commands;
using Libota.Application.Organisation.Events;
using Libota.Application.Organisation.Queries;

namespace Libota.Application.Organisation.Aggregates
{
    public class OrganisationAggregate : AggregateRoot<OrganisationAggregate, OrganisationId>,
        IEmit<OrganisationCreated>,
        IEmit<MemberRegistered>,
        IEmit<MemberRegistrationUpdated>,
        IEmit<MemberRegistrationEnded>
    {
        private Domain.Organisation.Entities.Organisation? _state;
        private readonly IQueryProcessor _queryProcessor;

        public OrganisationAggregate(OrganisationId id, IQueryProcessor queryProcessor) : base(id)
        {
            _queryProcessor = queryProcessor;
            Register<OrganisationCreated>(Apply);
            Register<MemberRegistered>(Apply);
            Register<MemberRegistrationUpdated>(Apply);
            Register<MemberRegistrationEnded>(Apply);
        }

        public async Task<IExecutionResult> CreateOrganisation(CreateOrganisationCommand command)
        {
            if (_state != null)
                return await Task.FromResult(ExecutionResult.Failed("organisation already created"));

            var conflictingOrganisation =
                _queryProcessor.ProcessAsync(new GetOrganisationByName(command.Name), CancellationToken.None).Result;

            if (conflictingOrganisation != null)
            {
                return await Task.FromResult(
                    ExecutionResult.Failed($"an organisation with the name {command.Name} exists already"));
            }

            Emit(new OrganisationCreated(command.Name, command.Description));
            return await Task.FromResult(ExecutionResult.Success());
        }

        public async Task<IExecutionResult> RegisterMember(RegisterMemberCommand command)
        {
            if (_state == null)
                return await Task.FromResult(ExecutionResult.Failed($"the organisation must be created first"));
            try
            {
                var request = command.RegistrationRequest;
                var registeredEvent = new MemberRegistered
                {
                    FirstName = request.FirstName,
                    MiddleName = request.MiddleName,
                    LastName = request.LastName,
                    Gender = request.Gender,
                    RegistrationBegin = request.RegistrationBegin.GetValueOrDefault(),
                    MembershipType = request.MembershipType,
                };
                Emit(registeredEvent);
                return await Task.FromResult(ExecutionResult.Success());
            }
            catch (Exception ex)
            {
                return await Task.FromResult(ExecutionResult.Failed($"{ex.Message}"));
            }
        }

        public void Apply(MemberRegistered aggregateEvent)
        {
            var newMember = new Member(aggregateEvent.FirstName, aggregateEvent.MiddleName, aggregateEvent.LastName,
                aggregateEvent.Gender, DateTime.MinValue);
            var registration = _state?.Register(newMember, aggregateEvent.MembershipType,
                aggregateEvent.RegistrationBegin);
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
            _state = new Domain.Organisation.Entities.Organisation(aggregateEvent.Name, aggregateEvent.Description);
        }
    }
}