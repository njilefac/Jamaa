using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Members.Queries;
using Libota.Data.Configuration;
using Libota.Data.Models.Members;
using Microsoft.EntityFrameworkCore;

namespace Libota.Data.Queries.Members;

public class MembersQueryHandler(LibotaDbContext dbContext) : IMembersQueryHandler
{
    public async Task<IList<MemberData>> Get(GetMembersByOrganisation query)
    {
        var organisations = await dbContext.Organisations
            .Include(x => x.Members)
            .ThenInclude(member => member.Registration)
            .ToListAsync();

        var matchingOrganisation = organisations.FirstOrDefault(x => x.Id != null && x.Id == query.OrganisationId.Value);

        return matchingOrganisation != null ? matchingOrganisation.Members : new List<MemberData>();
    }
}