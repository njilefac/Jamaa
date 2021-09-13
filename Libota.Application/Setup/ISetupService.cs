using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Users;
using Libota.Application.Organisation.Queries.Models;

namespace Libota.Application.Setup
{
    public interface ISetupService
    {
        Task<User?> GetSuperUser();
        Task<User?> CreateSuperUser(string username, string password, string? email, string? firstName,
            string? lastName);
        Task<bool> CreateOrganisation(string name, string? description);

        Task<IEnumerable<OrganisationReadModel>> ListOrganisations();
    }
}