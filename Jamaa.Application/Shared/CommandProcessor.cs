using Akka.Actor;
using Domain.Organisation.Values;
using Jamaa.Application.Organisation.Aggregates;
using Jamaa.Application.Organisation.Commands;

namespace Jamaa.Application.Shared;

public class CommandProcessor : ReceiveActor
{
    private readonly IQueryProcessor _queryProcessor;

    public CommandProcessor(IQueryProcessor queryProcessor)
    {
        _queryProcessor = queryProcessor;
        ReceiveAsync<CreateOrganisation>(OnCreateOrganisation);
        ReceiveAsync<RegisterMember>(OnRegisterMember);
        ReceiveAsync<UpdateMember>(OnUpdateMember);
    }

    private Task OnUpdateMember(UpdateMember command)
    {
        var organisation = Context.ActorOf(OrganisationAggregate.Props(command.OrganisationId, _queryProcessor));
        organisation.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnRegisterMember(RegisterMember command)
    {
        var organisation = Context.ActorOf(OrganisationAggregate.Props(command.OrganisationId, _queryProcessor));
        organisation.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnCreateOrganisation(CreateOrganisation createCommand)
    {
        var organisation = Context.ActorOf(OrganisationAggregate.Props(OrganisationId.With(Guid.NewGuid()), _queryProcessor));
        organisation.Tell(createCommand);
        return Task.CompletedTask;
    }
}