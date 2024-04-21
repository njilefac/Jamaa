using System;
using System.Threading.Tasks;
using Akka.Actor;
using Domain.Organisation.Values;
using Libota.Application.Organisation.Aggregates;
using Libota.Application.Organisation.Commands;

namespace Libota.Application.Shared;

public class CommandProcessor : ReceiveActor
{
    private readonly IServiceProvider _serviceProvider;

    public CommandProcessor(IServiceProvider sp)
    {
        _serviceProvider = sp;
        ReceiveAsync<CreateOrganisation>(OnCreateOrganisation);
    }

    private Task OnCreateOrganisation(CreateOrganisation createCommand)
    {
        return Task.Run(() =>
        {
            var queryProcessor = Context.ActorOf(QueryProcessor.Props(_serviceProvider));
            var organisation = Context.ActorOf(OrganisationAggregate.Props(OrganisationId.With(Guid.NewGuid()), queryProcessor));
            organisation.Tell(createCommand);
        });
    }
}