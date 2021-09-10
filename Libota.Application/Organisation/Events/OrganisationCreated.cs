using EventFlow.Aggregates;
using EventFlow.EventStores;
using Libota.Application.Organisation.Aggregates;

namespace Libota.Application.Organisation.Events
{
    [EventVersion("organisation-created", 1)]
    public class OrganisationCreated : AggregateEvent<OrganisationAggregate, OrganisationId>
    {
        public string Name { get; }
        public string? Description { get; }

        public OrganisationCreated(string name, string? description)
        {
            Name = name;
            Description = description;
        }
    }
}