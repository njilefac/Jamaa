using System.ComponentModel.DataAnnotations;
using EventFlow.Aggregates;
using EventFlow.ReadStores;
using Libota.Application.Organisation.Aggregates;
using Libota.Application.Organisation.Events;
using Libota.Application.Organisation.Events.Members;

namespace Libota.Application.Organisation.Models
{
    public class OrganisationReadModel : IReadModel,
        IAmReadModelFor<OrganisationAggregate, OrganisationId, MemberRegistrationUpdated>,
        IAmReadModelFor<OrganisationAggregate, OrganisationId, OrganisationCreated>
    {
        public string Description { get; private set; }

        public string Name { get; private set; }
        [Key] public string Id { get; set; }

        public void Apply(IReadModelContext context,
            IDomainEvent<OrganisationAggregate, OrganisationId, MemberRegistrationUpdated> domainEvent)
        {
            throw new System.NotImplementedException();
        }

        public void Apply(IReadModelContext context,
            IDomainEvent<OrganisationAggregate, OrganisationId, OrganisationCreated> domainEvent)
        {
            Name = domainEvent.AggregateEvent.Name;
            Description = domainEvent.AggregateEvent.Description;
        }
    }
}