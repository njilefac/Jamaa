using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.EntityFramework;
using EventFlow.Queries;
using Libota.Application.Organisation.Models;
using Libota.Application.Organisation.Queries;
using Libota.Data.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Libota.Data.Models.ReadModels.Handlers
{
    public class OrganisationQueryHandler : IQueryHandler<GetAllOrganisations, IEnumerable<OrganisationReadModel>>
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
    }
}