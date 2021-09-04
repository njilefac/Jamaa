using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using EventFlow.Core;

namespace Libota.Application.Organisation.Commands
{
    public class CreateOrganisationCommand : Command<OrganisationAggregate,OrganisationIdentity, ExecutionResult>
    {
        public string Name { get; }
        public string Description { get; }

        public CreateOrganisationCommand(OrganisationIdentity aggregateId, string name, string description) : base(aggregateId)
        {
            Name = name;
            Description = description;
        }

        public CreateOrganisationCommand(OrganisationIdentity aggregateId, ISourceId sourceId) : base(aggregateId, sourceId)
        {
        }
    }
}