using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Members.Queries;
using Jamaa.Data.Models.Members;

namespace Jamaa.Data.Queries.Members;

public interface IMembersQueryHandler
{
    Task<IList<MemberData>> Get(GetMembersByOrganisation query);
}