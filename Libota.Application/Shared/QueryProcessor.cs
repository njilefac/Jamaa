using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Domain.Organisation.Queries;
using Libota.Data.Models.Organisation;
using Libota.Data.Queries.Organisation.Handlers;

namespace Libota.Application.Shared;

public class QueryProcessor : ReceiveActor
{
    private readonly IActorRef _organisationQueryHandler;

    public QueryProcessor(IServiceProvider sp)
    {
        _organisationQueryHandler = Context.ActorOf(OrganisationQueryHandler.Props(sp), "organisation-query-handler");

        ReceiveAsync<GetAllOrganisations>(OnGetAllOrganisations);
    }

    private async Task<IEnumerable<OrganisationReadModel>> OnGetAllOrganisations(GetAllOrganisations message)
    {
        return await _organisationQueryHandler.Ask<IEnumerable<OrganisationReadModel>>(message);
    }

    public static Props Props(IServiceProvider sp)
    {
        return new Props(typeof(QueryProcessor), [sp]);
    }
}