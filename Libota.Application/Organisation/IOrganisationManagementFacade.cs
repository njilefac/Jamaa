using System.Collections.Generic;
using System.Threading.Tasks;
using Libota.Application.Organisation.Queries.Models;
using Libota.Application.Organisation.Requests;

namespace Libota.Application.Organisation
{
    public interface IOrganisationManagementFacade
    {
        Task<bool> CreateOrganisation(string name, string? description);
        Task RegisterMember(MemberRegistrationRequest request);
        Task<IList<OrganisationReadModel>> ListOrganisations();
    }
}