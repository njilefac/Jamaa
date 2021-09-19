using System.Collections.Generic;
using EventFlow.Queries;
using Libota.Application.Members.Queries.Models;
using Libota.Application.Organisation.Aggregates;

namespace Libota.Application.Members.Queries
{
    public class GetMembersByOrganisation : IQuery<IEnumerable<Member>>
    {
        public GetMembersByOrganisation(OrganisationId organisationId)
        {
            OrganisationId = organisationId;
        }

        public OrganisationId OrganisationId { get; }
    }
}