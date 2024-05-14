using System;
using System.Collections.Generic;
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
    private readonly IQueryProcessor _queryProcessor;

    public OrganisationManagementFacade(IRequiredActor<CommandProcessor> commandProcessorProvider,
        IQueryProcessor queryProcessor,
        IDataChangeNotifier dataChangeNotifier,
        IUserSessionService userSessionService)
    {
        _userSessionService = userSessionService;
        _commandProcessor = commandProcessorProvider.ActorRef;
        _queryProcessor = queryProcessor;

        MemberAdded = CreateMemberAddedObservable().Merge(dataChangeNotifier.Insertions.OfType<MemberData>());
        MemberUpdated = dataChangeNotifier.Updates.OfType<MemberData>();
        MemberDeleted = dataChangeNotifier.Deletions.OfType<MemberData>();
    }

    public Task CreateOrganisation(string name, string? description)
    {
        return Task.Run(() => _commandProcessor.Tell(new CreateOrganisation(name, description)));
    }

    [Authorize(Operation = "member.registration")]
    public Task RegisterMember(MemberRegistrationRequest request)
    {
        return Task.Run(() =>
        {
            var message = new RegisterMember(request.OrganisationId, 
                request.FirstName, 
                request.MiddleName, 
                request.LastName, 
                request.Gender,
                request.MembershipType, 
                request.RegistrationBegin);
            
            _commandProcessor.Tell(message);
        });
    }

    public async Task<IEnumerable<OrganisationData>> ListOrganisations()
    {
        return await _queryProcessor.Get(new GetAllOrganisations());
    }

    private async Task<IList<MemberData>?> ListCurrentMembers()
    {
        var currentOrganisationId = _userSessionService.CurrentUserSession?.Organisation?.Id;
        var query = new GetMembersByOrganisation(OrganisationId.With(currentOrganisationId ?? Guid.NewGuid().ToString()));
        return await _queryProcessor.Get(query);
    }

    public IObservable<MemberData> MemberAdded { get; }

    public IObservable<MemberData> MemberUpdated { get; }

    public IObservable<MemberData> MemberDeleted { get; }

    private IObservable<MemberData> CreateMemberAddedObservable()
    {
        return (ListCurrentMembers().Result ?? throw new InvalidOperationException()).ToObservable();
    }
}