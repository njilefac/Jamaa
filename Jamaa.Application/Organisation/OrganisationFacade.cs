using System.Reactive.Linq;
using System.Reactive.Subjects;
using Akka.Actor;
using Akka.Hosting;
using Domain.Members.Queries;
using Domain.Organisation.Queries;
using Domain.Organisation.Requests;
using Domain.Organisation.Values;
using Jamaa.Application.Members.Aggregates;
using Jamaa.Application.Organisation.Commands;
using Jamaa.Application.Security.Authorization;
using Jamaa.Application.Shared;
using Jamaa.Application.Users;
using Jamaa.Application.Users.Services;
using Jamaa.Data.Models.Members;
using Jamaa.Data.Models.Organisation;
using Jamaa.Data.Notifiers;

namespace Jamaa.Application.Organisation;

public class OrganisationFacade : IOrganisationFacade
{
    private readonly IActorRef _commandProcessor;
    private readonly ReplaySubject<MemberData> _currentMembers;
    private readonly IQueryProcessor _queryProcessor;

    public OrganisationFacade(IRequiredActor<CommandProcessor> commandProcessorProvider,
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

        MemberUpdated = dataChangeNotifier.Updates.OfType<MemberData>()
            .Merge(dataChangeNotifier.Updates.OfType<RegistrationData>().Select(r => r.Member));
        MemberDeleted = dataChangeNotifier.Deletions.OfType<MemberData>();
    }

    private IObservable<MemberData> MemberAdded { get; }

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

    [Authorize(Operation = "member.update")]
    public Task UpdateMember(MemberUpdateRequest request)
    {
        return Task.Run(() =>
        {
            var message = new UpdateMember(request.OrganisationId,
                new MemberId(request.MemberId),
                request.FirstName,
                request.MiddleName,
                request.LastName,
                request.Gender,
                request.MembershipType,
                request.Status,
                request.RegistrationBegin,
                request.RegistrationEnd,
                request.Avatar);

            _commandProcessor.Tell(message);
        });
    }

    public async Task<IEnumerable<OrganisationData>> ListOrganisations()
    {
        var organisations = await _queryProcessor.Get(new GetAllOrganisations());
        return organisations.Select(organisation => organisation.ToDataModel()).ToList();
    }

    public IObservable<MemberData> MemberUpdated { get; }

    public IObservable<MemberData> MemberDeleted { get; }

    private async void InitializeCurrentMembers(UserSession? s)
    {
        var existingMembers = (await ListCurrentMembers(s) ?? throw new InvalidOperationException()).ToObservable();
        foreach (var member in existingMembers) _currentMembers.OnNext(member);
    }

    private async Task<IList<MemberData>?> ListCurrentMembers(UserSession? userSession)
    {
        var currentOrganisationId = userSession?.Organisation?.Id;
        var query = new GetMembersByOrganisation(
            OrganisationId.With(currentOrganisationId ?? Guid.NewGuid().ToString()));
        var existingMembers = await _queryProcessor.Get(query);
        return existingMembers.Select(member => member.ToDataModel()).ToList();
    }
}