using System;
using System.Threading.Tasks;
using Akka.Actor;
using Domain.Organisation.Values;
using Libota.Application.Organisation.Aggregates;
using Libota.Application.Organisation.Commands;

namespace Libota.Application.Shared;

public class CommandProcessor : ReceiveActor
{
    private readonly IQueryProcessor _queryProcessor;

    public CommandProcessor(IQueryProcessor queryProcessor)
    {
        _queryProcessor = queryProcessor;
        ReceiveAsync<CreateOrganisation>(OnCreateOrganisation);
    }

    private Task OnCreateOrganisation(CreateOrganisation createCommand)
    {
        var organisation = Context.ActorOf(OrganisationAggregate.Props(OrganisationId.With(Guid.NewGuid()), _queryProcessor));
        organisation.Tell(createCommand);
        return Task.CompletedTask;
    }
}