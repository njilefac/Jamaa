using System;
using EventFlow.Core;

namespace Libota.Application.Organisation
{
    public class OrganisationIdentity : Identity<OrganisationIdentity>
    {
        public OrganisationIdentity(Guid value) : base(value.ToString())
        {
        }
    }
}