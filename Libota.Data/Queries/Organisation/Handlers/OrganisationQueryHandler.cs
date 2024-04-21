using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Domain.Organisation.Queries;
using Libota.Data.Configuration;
using Libota.Data.Models.Organisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Libota.Data.Queries.Organisation.Handlers;

public class OrganisationQueryHandler : ReceiveActor
{
    private readonly LibotaDbContext _dbContext;

    public OrganisationQueryHandler(LibotaDbContext dbContext)
    {
        _dbContext = dbContext;

        ReceiveAsync<GetAllOrganisations>(HandleMessage);
        ReceiveAsync<GetOrganisationByName>(HandleMessage);
    }

    private async Task<IEnumerable<OrganisationReadModel?>> HandleMessage(GetAllOrganisations query)
    {
        var result = await _dbContext.Organisations.ToListAsync();
        return result;
    }

    private async Task<OrganisationReadModel?> HandleMessage(GetOrganisationByName query)
    {
        var result =  await _dbContext.Organisations.SingleOrDefaultAsync(x => x.Name.Equals(query.Name, StringComparison.InvariantCultureIgnoreCase));
        return result;
    }

    public static Props Props(IServiceProvider sp)
    {
        return new Props(typeof(OrganisationQueryHandler), [sp.GetRequiredService<LibotaDbContext>()]);
    }
}