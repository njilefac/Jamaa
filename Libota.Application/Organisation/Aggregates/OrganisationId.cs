using EventFlow.Core;

namespace Libota.Application.Organisation.Aggregates
{
    public class OrganisationId : Identity<OrganisationId>
    {
        public OrganisationId(string value) : base(value)
        {
        }
    }
}