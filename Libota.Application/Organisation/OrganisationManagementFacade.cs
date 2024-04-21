using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Hosting;
using Domain.Members.Queries;
using Domain.Organisation.Queries;
using Domain.Organisation.Requests;
using Domain.Organisation.Values;
using Libota.Application.Organisation.Commands;
using Libota.Application.Security;
using Libota.Application.Shared;
using Libota.Application.Users.Services;
using Libota.Data.Models.Members;
using Libota.Data.Models.Organisation;
using Libota.Data.Notifiers;

namespace Libota.Application.Organisation;

public class OrganisationManagementFacade : IOrganisationManagementFacade
{
    private readonly IUserSessionService _userSessionService;
    private readonly IActorRef _commandProcessor;
    private readonly IActorRef _queryProcessor;

    public OrganisationManagementFacade(IRequiredActor<CommandProcessor> commandProcessorProvider,
        IRequiredActor<QueryProcessor> queryProcessorProvider,
        IDataChangeNotifier dataChangeNotifier,
        IUserSessionService userSessionService)
    {
        _userSessionService = userSessionService;
        _commandProcessor = commandProcessorProvider.ActorRef;
        _queryProcessor = queryProcessorProvider.ActorRef;

        MemberAdded = CreateMemberAddedObservable().Merge(dataChangeNotifier.Insertions.OfType<Member>());
        MemberUpdated = dataChangeNotifier.Updates.OfType<Member>();
        MemberDeleted = dataChangeNotifier.Deletions.OfType<Member>();
    }

       

    public Task CreateOrganisation(string name, string? description)
    {
        return Task.Run(() => _commandProcessor.Tell(new CreateOrganisation(name, description)));
    }

    [Authorize(Operation = "member.registration")]
    public Task RegisterMember(MemberRegistrationRequest request)
    {
        return Task.Run(() =>  _commandProcessor.Tell(new RegisterMember(request)));
    }

    public async Task<IList<OrganisationReadModel>> ListOrganisations()
    {
        return await _queryProcessor.Ask<IList<OrganisationReadModel>>(new GetAllOrganisations());
    }

    private async Task<IList<Member>?> ListCurrentMembers()
    {
        var currentOrganisationId = _userSessionService.CurrentUserSession?.Organisation?.Id;
        var query = new GetMembersByOrganisation(OrganisationId.With(currentOrganisationId ?? throw new InvalidOperationException()));
        return await _queryProcessor.Ask<IList<Member>?>(query);
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