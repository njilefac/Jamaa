using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Organisation.Queries;
using Jamaa.Data.Models.Organisation;

namespace Jamaa.Data.Repositories.Organisations;

public interface IOrganisationQueryHandler
{
    Task<List<OrganisationData>> HandleQuery(GetAllOrganisations query);
    Task<OrganisationData?> HandleQuery(GetOrganisationByName query);
}