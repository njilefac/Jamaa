using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Users;
using Libota.Data.Models.Organisation;

namespace Libota.Application.Setup
{
    public interface ISetupService
    {
        Task<IEnumerable<OrganisationReadModel>> ListOrganisations();
        Task<bool> CreateOrganisation(string name, string? description);
        Task<User?> GetSuperUser();
        Task<User?> CreateSuperUser(string username, string password, string? email, string? firstName,
            string? lastName);
    }
}