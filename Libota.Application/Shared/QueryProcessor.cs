using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Members.Queries;
using Domain.Organisation.Queries;
using Libota.Data.Models.Members;
using Libota.Data.Models.Organisation;
using Libota.Data.Queries.Members;
using Libota.Data.Repositories.Organisations;

namespace Libota.Application.Shared;

public class QueryProcessor(IOrganisationQueryHandler organisationQueryHandler, IMembersQueryHandler membersQueryHandler)
    : IQueryProcessor
{
    public Task<List<OrganisationReadModel>> Get(GetAllOrganisations query) =>
        organisationQueryHandler.HandleQuery(query);

    public Task<OrganisationReadModel?> Get(GetOrganisationByName query) =>
        organisationQueryHandler.HandleQuery(query);

    public Task<IList<Member>> Get(GetMembersByOrganisation query) =>
        membersQueryHandler.Get(query);
}