using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.EntityFramework;
using EventFlow.Queries;
using Libota.Application.Members.Queries;
using Libota.Application.Members.Queries.Models;
using Libota.Data.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Libota.Data.Queries.Members
{
    public class MembersQueryHandler : IQueryHandler<GetMembersByOrganisation, IEnumerable<Member>>
    {
        private readonly LibotaDbContext _dataContext;

        public MembersQueryHandler(IDbContextProvider<LibotaDbContext> dbContextProvider)
        {
            _dataContext = dbContextProvider.CreateContext();
        }
        public async Task<IEnumerable<Member>> ExecuteQueryAsync(GetMembersByOrganisation query, CancellationToken cancellationToken)
        {
            var organisations = await _dataContext.Organisations
                .Include(x => x.Members)
                .ThenInclude(member => member.Registration)
                .ToListAsync(cancellationToken);

            var matchingOrganisation = organisations.FirstOrDefault(x =>
                x.Id != null && x.Id.Equals(query.OrganisationId.Value, StringComparison.InvariantCultureIgnoreCase));

            return matchingOrganisation != null ? matchingOrganisation.Members : new List<Member>();
        }
    }
}