using System.Threading;
using System.Threading.Tasks;
using EventFlow;
using Libota.Application.Organisation.Commands;
using Libota.Application.Organisation.Requests;
using Libota.Application.Security;

namespace Libota.Application.Organisation
{
    public class OrganisationManagementFacade : IOrganisationManagementFacade
    {
        private readonly ICommandBus _commandBus;
        public OrganisationManagementFacade(ICommandBus commandBus)
        {
            _commandBus = commandBus;
        }
        [Authorize(Operation = "member.registration")]
        public async Task RegisterMember(MemberRegistrationRequest request)
        {
            await _commandBus.PublishAsync(new RegisterMemberCommand(request), CancellationToken.None);
        }
    }
}