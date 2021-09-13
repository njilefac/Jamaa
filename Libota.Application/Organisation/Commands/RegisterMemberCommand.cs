using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using Libota.Application.Organisation.Aggregates;
using Libota.Application.Organisation.Requests;

namespace Libota.Application.Organisation.Commands
{
    public class RegisterMemberCommand : Command<OrganisationAggregate, OrganisationId, IExecutionResult>
    {
        public MemberRegistrationRequest RegistrationRequest { get; }

        public RegisterMemberCommand(MemberRegistrationRequest registrationRequest) : base(registrationRequest.OrganisationId)
        {
            RegistrationRequest = registrationRequest;
        }
    }
}