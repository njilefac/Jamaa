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
    public Task<List<OrganisationData>> Get(GetAllOrganisations query) =>
        organisationQueryHandler.HandleQuery(query);

    public Task<OrganisationData?> Get(GetOrganisationByName query) =>
        organisationQueryHandler.HandleQuery(query);

    public Task<IList<MemberProfile>> Get(GetMembersByOrganisation query) =>
        membersQueryHandler.Get(query);
}