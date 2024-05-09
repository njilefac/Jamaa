using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Hosting;
using Akka.Persistence;
using Domain.Members;
using Domain.Organisation.Queries;
using Domain.Organisation.Values;
using Libota.Application.Members.Events;
using Libota.Application.Organisation.Commands;
using Libota.Application.Organisation.Events;
using Libota.Application.Shared;

namespace Libota.Application.Organisation.Aggregates
{
    public class OrganisationAggregate : ReceivePersistentActor
    {
        private Domain.Organisation.Entities.Organisation? _state;
        private readonly IQueryProcessor _queryProcessor;

        public OrganisationAggregate(OrganisationId id, IQueryProcessor queryProcessor)
        {
            _queryProcessor = queryProcessor;
            PersistenceId = $"organisation-{id.Value}";
        }
        
        protected override bool Receive(object message)
        {
            switch (message)
            {
                case CreateOrganisation command:
                    if (IsValid(command))
                        Persist(new OrganisationCreated(command.Name, command.Description), ApplyEvent);

                    break;
                case RegisterMember command:
                    if (IsValid(command))
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
                        Persist(registeredEvent, ApplyEvent);
                    }
                    break;
                default:
                    return false;
            }

            return true;
        }

        private bool IsValid(RegisterMember command)
        {
            if (_state != null) return true;
            Context.Sender.Tell($"the organisation must be created first");
            return false;
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

        private void ApplyEvent(MemberRegistered registered)
        {
            var newMember = new Member(registered.FirstName, registered.MiddleName, registered.LastName,
                registered.Gender, DateTime.MinValue);
            
            _state?.Register(newMember, registered.MembershipType,
                registered.RegistrationBegin);
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