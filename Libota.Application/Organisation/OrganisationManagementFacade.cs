using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow;
using EventFlow.Queries;
using Libota.Application.Members.Queries.Models;
using Libota.Application.Organisation.Commands;
using Libota.Application.Organisation.Queries;
using Libota.Application.Organisation.Queries.Models;
using Libota.Application.Organisation.Requests;
using Libota.Application.Security;
using Libota.Application.Shared.Providers;

namespace Libota.Application.Organisation
{
    public class OrganisationManagementFacade : IOrganisationManagementFacade
    {
        private readonly ICommandBus _commandBus;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IProvideObservableData<Member> _membersAddedEvents;
        public OrganisationManagementFacade(ICommandBus commandBus, IQueryProcessor queryProcessor, IProvideObservableData<Member> membersAddedEvents)
        {
            _commandBus = commandBus;
            _queryProcessor = queryProcessor;
            _membersAddedEvents = membersAddedEvents;
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
        public IObservable<Member> Members => _membersAddedEvents.Stream;
    }
}