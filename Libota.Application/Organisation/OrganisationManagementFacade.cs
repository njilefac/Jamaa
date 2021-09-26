using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
using Libota.Application.Shared.Providers;
using Libota.Application.Users.Services;

namespace Libota.Application.Organisation
{
    public class OrganisationManagementFacade : IOrganisationManagementFacade
    {
        private readonly IUserSessionService _userSessionService;
        private readonly ICommandBus _commandBus;
        private readonly IQueryProcessor _queryProcessor;

        public OrganisationManagementFacade(
            ICommandBus commandBus,
            IQueryProcessor queryProcessor,
            IDataChangeNotifier dataChangeNotifier,
            IUserSessionService userSessionService)
        {
            _commandBus = commandBus;
            _queryProcessor = queryProcessor;
            _userSessionService = userSessionService;

            MemberAdded = CreateMemberAddedObservable();
            MemberAdded = MemberAdded.Merge(dataChangeNotifier.Insertions.OfType<Member>());
            MemberUpdated = dataChangeNotifier.Updates.OfType<Member>();
            MemberDeleted = dataChangeNotifier.Deletions.OfType<Member>();
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

        private async Task<IList<Member>?> ListCurrentMembers()
        {
            var currentOrganisationId = _userSessionService.CurrentUserSession?.Organisation?.Id;
            var query = new GetMembersByOrganisation(OrganisationId.With(currentOrganisationId));
            var members = await _queryProcessor.ProcessAsync(query, CancellationToken.None);
            return members.ToList();
        }

        public IObservable<Member> MemberAdded { get; }

        public IObservable<Member> MemberUpdated { get; }

        public IObservable<Member> MemberDeleted { get; }

        private IObservable<Member> CreateMemberAddedObservable()
        {
            return Observable.Create<Member>(observer =>
                {
                    var seedData = ListCurrentMembers().Result;
                    if (seedData == null) return Disposable.Empty;
                    foreach (var member in seedData)
                    {
                        observer.OnNext(member);
                    }
                    return Disposable.Empty;
                }
            );
        }
    }
}