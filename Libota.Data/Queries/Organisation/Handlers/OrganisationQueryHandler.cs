using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.EntityFramework;
using EventFlow.Queries;
using Libota.Application.Organisation.Queries;
using Libota.Application.Organisation.Queries.Models;
using Libota.Data.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Libota.Data.Queries.Organisation.Handlers
{
    public class OrganisationQueryHandler : IQueryHandler<GetAllOrganisations, IEnumerable<OrganisationReadModel>>, 
        IQueryHandler<GetOrganisationByName, OrganisationReadModel>
    {
        private readonly LibotaDbContext _dbContext;

        public OrganisationQueryHandler(IDbContextProvider<LibotaDbContext> dbContextProvider)
        {
            _dbContext = dbContextProvider.CreateContext();
        }
        public async Task<IEnumerable<OrganisationReadModel>> ExecuteQueryAsync(GetAllOrganisations query, CancellationToken cancellationToken)
        {
            return await _dbContext.Organisations.ToListAsync(cancellationToken: cancellationToken);
        }

        public async Task<OrganisationReadModel> ExecuteQueryAsync(GetOrganisationByName query, CancellationToken cancellationToken)
        {
            var matches =  await _dbContext.Organisations.ToListAsync(cancellationToken: cancellationToken);
            return matches.SingleOrDefault(x => x.Name.Equals(query.Name, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}