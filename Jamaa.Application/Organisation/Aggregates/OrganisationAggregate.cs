using Akka.Actor;
using Akka.Persistence;
using Domain.Members;
using Domain.Organisation.Queries;
using Domain.Organisation.Values;
using Jamaa.Application.Members.Aggregates;
using Jamaa.Application.Members.Events;
using Jamaa.Application.Organisation.Commands;
using Jamaa.Application.Organisation.Events;
using Jamaa.Application.Shared;

namespace Jamaa.Application.Organisation.Aggregates
{
    public class OrganisationAggregate : ReceivePersistentActor
    {
        private Domain.Organisation.Entities.Organisation? _state;
        private readonly IQueryProcessor _queryProcessor;

        public OrganisationAggregate(OrganisationId id, IQueryProcessor queryProcessor)
        {
            _queryProcessor = queryProcessor;
            PersistenceId = id.Value;

            RegisterCommandsHandlers();
            RegisterEventHandlers();
        }

        private void RegisterEventHandlers()
        {
            Recover<SnapshotOffer>(offer =>
            {
                if (offer.Snapshot is Domain.Organisation.Entities.Organisation state)
                    _state = state;
            });
            
            Recover<OrganisationCreated>(ApplyEvent);
        }

        private void RegisterCommandsHandlers()
        {
            Command<CreateOrganisation>(command =>
            {
                if (IsValid(command))
                    Persist(new OrganisationCreated(OrganisationId.With(PersistenceId), command.Name, command.Description), ApplyEvent);
            });
            Command<RegisterMember>(command =>
            {
                if (!IsValid(command)) return;

                var registeredEvent = new MemberRegistered
                (
                    new MemberId(Guid.NewGuid().ToString()),
                    command.FirstName,
                    command.MiddleName,
                    command.LastName,
                    command.Gender,
                    BirthDate: null,
                    command.RegistrationBegin,
                    command.MembershipType,
                    OrganisationId.With(PersistenceId)
                );
                Persist(registeredEvent, ApplyEvent);
                if(LastSequenceNr % 5 == 0)
                    SaveSnapshot(_state);
            });
            Command<UpdateMember>(command =>
            {
                var updatedEvent = new MemberUpdated(
                    command.MemberId,
                    command.FirstName,
                    command.MiddleName,
                    command.LastName,
                    command.Gender,
                    BirthDate: null,
                    command.RegistrationBegin,
                    command.RegistrationEnd,
                    command.MembershipType,
                    command.Status,
                    OrganisationId.With(PersistenceId),
                    command.Avatar
                );
                Persist(updatedEvent, ApplyEvent);
                if(LastSequenceNr % 5 == 0)
                    SaveSnapshot(_state);
            });
        }

        private bool IsValid(RegisterMember command)
        {
            return _state != null &&
                   !_state.Members.Any(x => x.FirstName == command.FirstName &&
                                            x.LastName == command.LastName);
        }

        private bool IsValid(CreateOrganisation command)
        {
            if (_state != null)
            {
                Context.Sender.Tell("organisation already created", Self);
                return false;
            }

            var conflictingOrganisation = _queryProcessor.Get(new GetOrganisationByName(command.Name)).Result;
            if (conflictingOrganisation == null) return true;

            Context.Sender.Tell($"an organisation with the name {command.Name} already exists", Self);
            return false;
        }

        private void ApplyEvent(MemberUpdated updated)
        {
            // The state doesn't currently seem to store member IDs or offer a way to update them by ID.
            // In a real system, the Domain Entity should have an Update method.
        }

        private void ApplyEvent(MemberRegistered registered)
        {
            var newMember = new Member(registered.FirstName, registered.MiddleName, registered.LastName, registered.Gender, DateTime.MinValue);

            _state?.Register(newMember, registered.MembershipType, registered.RegistrationBegin);
        }

        private void ApplyEvent(OrganisationCreated created)
        {
            _state = new Domain.Organisation.Entities.Organisation(created.Name, created.Description);
        }

        public override string PersistenceId { get; }

        public static Props Props(OrganisationId id, IQueryProcessor queryProcessor)
        {
            return new Props(typeof(OrganisationAggregate), [id, queryProcessor]);
        }
    }
}