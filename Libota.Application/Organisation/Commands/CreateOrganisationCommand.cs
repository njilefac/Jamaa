using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using Libota.Application.Organisation.Aggregates;

namespace Libota.Application.Organisation.Commands
{
    public class CreateOrganisationCommand : Command<OrganisationAggregate,OrganisationId, IExecutionResult>
    {
        public string Name { get; }
        public string Description { get; }

        public CreateOrganisationCommand(OrganisationId aggregateId, string name, string description) : base(aggregateId)
        {
            Name = name;
            Description = description;
        }
    }
}