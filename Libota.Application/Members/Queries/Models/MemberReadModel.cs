using Domain.Values;
using EventFlow.Aggregates;
using EventFlow.ReadStores;
using Libota.Application.Members.Events;
using Libota.Application.Organisation.Aggregates;

namespace Libota.Application.Members.Queries.Models
{
    public class MemberReadModel : IAmReadModelFor<OrganisationAggregate, OrganisationId, MemberRegistered>
    {
        public void Apply(IReadModelContext context,
            IDomainEvent<OrganisationAggregate, OrganisationId, MemberRegistered> domainEvent)
        {
            OrganisationId = domainEvent.AggregateIdentity;
            FirstName = domainEvent.AggregateEvent.FirstName;
            MiddleName = domainEvent.AggregateEvent.MiddleName;
            LastName = domainEvent.AggregateEvent.LastName;
            Gender = domainEvent.AggregateEvent.Gender;
        }

        public string LastName { get; private set; }
        public string? MiddleName { get; private set; }
        public OrganisationId OrganisationId { get; private set; }
        public string FirstName { get; private set; }
        public Gender Gender { get; private set; }
    }
}