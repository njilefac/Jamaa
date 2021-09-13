using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using Libota.Application.Organisation.Aggregates;

namespace Libota.Application.Organisation.Commands.Handlers
{
    public class CreateOrganisationCommandHandler : CommandHandler<OrganisationAggregate, OrganisationId, IExecutionResult, CreateOrganisationCommand>
    {
        public override async Task<IExecutionResult> ExecuteCommandAsync(OrganisationAggregate aggregate, CreateOrganisationCommand command,
            CancellationToken cancellationToken)
        {
            return await aggregate.CreateOrganisation(command);
        }
    }
}