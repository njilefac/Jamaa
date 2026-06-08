using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Organisation.Queries;
using Jamaa.Data.Configuration;
using Jamaa.Data.Models.Organisation;
using Microsoft.EntityFrameworkCore;

namespace Jamaa.Data.Repositories.Organisations;

public class OrganisationQueryHandler(JamaaDbContext dbContext) : IOrganisationQueryHandler
{
    public Task<List<OrganisationData>> HandleQuery(GetAllOrganisations query)
    {
        return dbContext.Organisations.ToListAsync();
    }

    public Task<OrganisationData?> HandleQuery(GetOrganisationByName query)
    {
        return dbContext.Organisations.SingleOrDefaultAsync(x => x.Name == query.Name);
    }
}