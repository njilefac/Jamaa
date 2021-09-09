using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using Libota.Application.Organisation.Aggregates;

namespace Libota.Application.Organisation.Commands
{
    public class CreateOrganisationCommandHandler : CommandHandler<OrganisationAggregate, OrganisationId, IExecutionResult, CreateOrganisationCommand>
    {
        public override async Task<IExecutionResult> ExecuteCommandAsync(OrganisationAggregate aggregate, CreateOrganisationCommand command,
            CancellationToken cancellationToken)
        {
            var result = aggregate.CreateOrganisation(command.Name, command.Description);
            return await Task.FromResult(result);
        }
    }
}