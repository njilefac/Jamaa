using EventFlow.Core;
using EventFlow.Queries;
using Libota.Application.Organisation.Queries.Models;

namespace Libota.Application.Organisation.Queries
{
    public class GetOrganisationById : ReadModelByIdQuery<OrganisationReadModel>
    {
        public GetOrganisationById(IIdentity identity) : base(identity)
        {
        }

        public GetOrganisationById(string id) : base(id)
        {
        }
    }
}