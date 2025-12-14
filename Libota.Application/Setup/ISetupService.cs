using Domain.Users;
using Libota.Data.Models.Organisation;

namespace Libota.Application.Setup
{
    public interface ISetupService
    {
        Task<IEnumerable<OrganisationData>> ListOrganisations();
        Task<bool> CreateOrganisation(string name, string? description);
        Task<User?> GetSuperUser();
        Task<User?> CreateSuperUser(string username, string password, string? email, string? firstName,
            string? lastName);
    }
}