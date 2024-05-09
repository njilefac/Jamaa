using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Members.Queries;
using Libota.Data.Models.Members;

namespace Libota.Data.Queries.Members;

public interface IMembersQueryHandler
{
    Task<IList<MemberData>> Get(GetMembersByOrganisation query);
}