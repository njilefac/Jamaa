using Domain.Organisation.Values;
using Domain.Shared;
using Libota.Application.Organisation.Aggregates;

namespace Libota.Application.Organisation.Commands
{
    public class UpdateOrganisationContactDetails(OrganisationId organisationId, Address address)
    {
        public OrganisationId OrganisationId { get; } = organisationId;
        public Address Address { get; } = address;
    }
}