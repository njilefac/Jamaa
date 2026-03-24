using Domain.Organisation.Values;
using Domain.Shared;

namespace Jamaa.Application.Organisation.Commands
{
    public class UpdateOrganisationContactDetails(OrganisationId organisationId, Address address)
    {
        public OrganisationId OrganisationId { get; } = organisationId;
        public Address Address { get; } = address;
    }
}