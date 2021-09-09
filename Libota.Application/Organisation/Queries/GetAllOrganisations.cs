using System.Collections.Generic;
using EventFlow.Queries;
using Libota.Application.Organisation.Models;

namespace Libota.Application.Organisation.Queries
{
    public class GetAllOrganisations : IQuery<IEnumerable<OrganisationReadModel>>
    {
    }
}