using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Members.Queries;
using Domain.Organisation.Queries;
using Libota.Data.Models.Members;
using Libota.Data.Models.Organisation;

namespace Libota.Application.Shared;

public interface IQueryProcessor
{
    Task<List<OrganisationReadModel>> Get(GetAllOrganisations query);
    Task<OrganisationReadModel?> Get(GetOrganisationByName query);

    Task<IList<Member>> Get(GetMembersByOrganisation query);
}