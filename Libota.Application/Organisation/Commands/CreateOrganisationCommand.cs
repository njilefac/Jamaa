using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using Libota.Application.Organisation.Aggregates;

namespace Libota.Application.Organisation.Commands
{
    public class CreateOrganisationCommand : Command<OrganisationAggregate,OrganisationId, IExecutionResult>
    {
        public string Name { get; }
        public string? Description { get; }

        public CreateOrganisationCommand(string name, string? description) : base(OrganisationId.NewComb())
        {
            Name = name;
            Description = description;
        }
    }
}