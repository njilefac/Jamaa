using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using Libota.Application.Organisation.Aggregates;

namespace Libota.Application.Organisation.Commands.Handlers
{
    public class RegisterMemberCommandHandler : CommandHandler<OrganisationAggregate, OrganisationId, IExecutionResult, RegisterMemberCommand>
    {
        public override async Task<IExecutionResult> ExecuteCommandAsync(OrganisationAggregate aggregate, RegisterMemberCommand command,
            CancellationToken cancellationToken)
        {
            return await aggregate.RegisterMember(command); 
        }
    }
}