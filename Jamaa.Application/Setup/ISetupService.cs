using Domain.Users;
using Jamaa.Data.Models.Organisation;

namespace Jamaa.Application.Setup;

public interface ISetupService
{
    Task<IEnumerable<OrganisationData>> ListOrganisations();
    Task<bool> CreateOrganisation(string name, string? description);
    Task<User?> GetSuperUser();

    Task<User?> CreateSuperUser(string username, string password, string? email, string? firstName,
        string? lastName);
}