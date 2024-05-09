using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Organisation.Queries;
using Libota.Data.Models.Organisation;

namespace Libota.Data.Repositories.Organisations;

public interface IOrganisationQueryHandler
{
    Task<List<OrganisationData>> HandleQuery(GetAllOrganisations query);
    Task<OrganisationData?> HandleQuery(GetOrganisationByName query);
}