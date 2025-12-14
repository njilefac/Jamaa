using System.Reactive.Linq;
using System.Reactive.Subjects;
using Akka.Actor;
using Akka.Hosting;
using Domain.Members.Queries;
using Domain.Organisation.Queries;
using Domain.Organisation.Requests;
using Domain.Organisation.Values;
using Libota.Application.Organisation.Commands;
using Libota.Application.Security;
using Libota.Application.Security.Authorization;
using Libota.Application.Shared;
using Libota.Application.Users;
using Libota.Application.Users.Services;
using Libota.Data.Models.Members;
using Libota.Data.Models.Organisation;
using Libota.Data.Notifiers;

namespace Libota.Application.Organisation;

public class OrganisationManagementFacade : IOrganisationManagementFacade
{
    private readonly IActorRef _commandProcessor;
    private readonly IQueryProcessor _queryProcessor;
    private readonly ReplaySubject<MemberData> _currentMembers;

    public OrganisationManagementFacade(IRequiredActor<CommandProcessor> commandProcessorProvider,
        IQueryProcessor queryProcessor,
        IDataChangeNotifier dataChangeNotifier,
        IUserSessionService userSessionService)
    {
        userSessionService.UserSessions.Subscribe(InitializeCurrentMembers);
        _commandProcessor = commandProcessorProvider.ActorRef;
        _queryProcessor = queryProcessor;

        _currentMembers = new ReplaySubject<MemberData>();
        CurrentMembers = _currentMembers;
        
        MemberAdded = dataChangeNotifier.Insertions.OfType<MemberData>();
        MemberAdded.Subscribe(x => _currentMembers.OnNext(x));
        
        MemberUpdated = dataChangeNotifier.Updates.OfType<MemberData>();
        MemberDeleted = dataChangeNotifier.Deletions.OfType<MemberData>();
    }

    private async void InitializeCurrentMembers(UserSession? s)
    {
        var existingMembers =  (await ListCurrentMembers(s) ?? throw new InvalidOperationException()).ToObservable();
        foreach (var member in existingMembers)
        {
            _currentMembers.OnNext(member);
        }
    }

    public IObservable<MemberData> CurrentMembers { get; set; }

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
    
    public IObservable<MemberData> MemberUpdated { get; }

    public IObservable<MemberData> MemberDeleted { get; }

    private async Task<IList<MemberData>?> ListCurrentMembers(UserSession? userSession)
    {
        var currentOrganisationId = userSession?.Organisation?.Id;
        var query = new GetMembersByOrganisation(OrganisationId.With(currentOrganisationId ?? Guid.NewGuid().ToString()));
        var existingMembers = await _queryProcessor.Get(query);
        return existingMembers;
    }

    private IObservable<MemberData> MemberAdded { get; }
}