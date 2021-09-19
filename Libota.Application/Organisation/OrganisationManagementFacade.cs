using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow;
using EventFlow.Queries;
using Libota.Application.Members.Queries;
using Libota.Application.Members.Queries.Models;
using Libota.Application.Organisation.Aggregates;
using Libota.Application.Organisation.Commands;
using Libota.Application.Organisation.Queries;
using Libota.Application.Organisation.Queries.Models;
using Libota.Application.Organisation.Requests;
using Libota.Application.Security;

namespace Libota.Application.Organisation
{
    public class OrganisationManagementFacade : IOrganisationManagementFacade
    {
        private readonly ICommandBus _commandBus;
        private readonly IQueryProcessor _queryProcessor;
        public OrganisationManagementFacade(ICommandBus commandBus, IQueryProcessor queryProcessor)
        {
            _commandBus = commandBus;
            _queryProcessor = queryProcessor;
        }

        public async Task<bool> CreateOrganisation(string name, string? description)
        {
            var result = await _commandBus.PublishAsync(
                new CreateOrganisationCommand(name, description), CancellationToken.None);

            return result.IsSuccess;
        }

        [Authorize(Operation = "member.registration")]
        public async Task RegisterMember(MemberRegistrationRequest request)
        {
            await _commandBus.PublishAsync(new RegisterMemberCommand(request), CancellationToken.None);
        }

        public async Task<IList<OrganisationReadModel>> ListOrganisations()
        {
            var result = await _queryProcessor.ProcessAsync(new GetAllOrganisations(), CancellationToken.None);
            return result.ToList();
        }

        public async Task<IList<Member>?> ListMembersByOrganisation(OrganisationId organisationId)
        {
            var query = new GetMembersByOrganisation(organisationId);
            var members = await _queryProcessor.ProcessAsync(query ,CancellationToken.None);
            return members.ToList();
        }
    }
}