using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Organisation.Queries;
using Libota.Data.Configuration;
using Libota.Data.Models.Organisation;
using Microsoft.EntityFrameworkCore;

namespace Libota.Data.Repositories.Organisations;

public class OrganisationQueryHandler(LibotaDbContext dbContext) : IOrganisationQueryHandler
{
    public Task<List<OrganisationData>> HandleQuery(GetAllOrganisations query) =>
        dbContext.Organisations.ToListAsync();

    public Task<OrganisationData?> HandleQuery(GetOrganisationByName query) =>
        dbContext.Organisations.SingleOrDefaultAsync(x => x.Name == query.Name);
}