using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EventFlow.Aggregates;
using EventFlow.ReadStores;
using Libota.Application.Members.Events;
using Libota.Application.Members.Queries.Models;
using Libota.Application.Organisation.Aggregates;
using Libota.Application.Organisation.Events;
using Libota.Application.Shared;

namespace Libota.Application.Organisation.Queries.Models
{
    public class OrganisationReadModel : IReadModel,
        IAmReadModelFor<OrganisationAggregate, OrganisationId, OrganisationCreated>,
        IAmReadModelFor<OrganisationAggregate, OrganisationId, MemberRegistered>,
        IAmReadModelFor<OrganisationAggregate, OrganisationId, MemberRegistrationUpdated>
    {
        public OrganisationReadModel()
        {
            Members = new List<Member>();
        }

        public string? Description { get; private set; }

        public string Name { get; private set; } = string.Empty;
        [Key] public string? Id { get; set; }

        public IList<Member> Members { get; set; }


        public void Apply(IReadModelContext context,
            IDomainEvent<OrganisationAggregate, OrganisationId, OrganisationCreated> domainEvent)
        {
            Name = domainEvent.AggregateEvent.Name;
            Description = domainEvent.AggregateEvent.Description;
        }

        public void Apply(IReadModelContext context,
            IDomainEvent<OrganisationAggregate, OrganisationId, MemberRegistered> domainEvent)
        {
            var newMember = new Member
            {
                FirstName = domainEvent.AggregateEvent.FirstName,
                MiddleName = domainEvent.AggregateEvent.MiddleName,
                LastName = domainEvent.AggregateEvent.LastName,
                Gender = domainEvent.AggregateEvent.Gender,
                Organisation = this,
            };

            Members.Add(newMember);
        }

        public void Apply(IReadModelContext context,
            IDomainEvent<OrganisationAggregate, OrganisationId, MemberRegistrationUpdated> domainEvent)
        {
            throw new NotImplementedException();
        }
    }
}