using Domain.Members.Queries;
using Domain.Organisation.Queries;
using Libota.Data.Models.Members;
using Libota.Data.Models.Organisation;

namespace Libota.Application.Shared;

public interface IQueryProcessor
{
    Task<List<OrganisationData>> Get(GetAllOrganisations query);
    Task<OrganisationData?> Get(GetOrganisationByName query);

    Task<IList<MemberProfile>> Get(GetMembersByOrganisation query);
}