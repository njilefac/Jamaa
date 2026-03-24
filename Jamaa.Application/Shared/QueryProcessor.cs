using Domain.Members.Queries;
using Domain.Organisation.Queries;
using Jamaa.Data.Models.Members;
using Jamaa.Data.Models.Organisation;
using Jamaa.Data.Queries.Members;
using Jamaa.Data.Repositories.Organisations;

namespace Jamaa.Application.Shared;

public class QueryProcessor(IOrganisationQueryHandler organisationQueryHandler, IMembersQueryHandler membersQueryHandler)
    : IQueryProcessor
{
    public Task<List<OrganisationData>> Get(GetAllOrganisations query) =>
        organisationQueryHandler.HandleQuery(query);

    public Task<OrganisationData?> Get(GetOrganisationByName query) =>
        organisationQueryHandler.HandleQuery(query);

    public Task<IList<MemberData>> Get(GetMembersByOrganisation query) =>
        membersQueryHandler.Get(query);
}