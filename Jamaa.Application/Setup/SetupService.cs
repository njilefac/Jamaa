using Domain.Users;
using Jamaa.Application.Organisation;
using Jamaa.Application.Users.Services;
using Jamaa.Data.Models.Organisation;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Jamaa.Application.Setup;

[UsedImplicitly]
public class SetupService(
    IUserRepository users,
    IOrganisationFacade organisationFacade,
    ILogger<UserManagementFacade> logger)
    : ISetupService
{
    public async Task<User?> GetSuperUser()
    {
        return await users.SingleOrDefault(x => x.Account.IsSuperUser != null &&
                                                 x.Account.IsSuperUser.Value);
    }

    public async Task<User?> CreateSuperUser(string username, string password, string? email, string? firstName,
        string? lastName)
    {
        logger.LogInformation("creating super user account...");
        var newSuperUser = new User(username, password, email, firstName, string.Empty, lastName,
            true);
        var superUser = await users.Add(newSuperUser);
        logger.LogInformation("super user account created!");
        return superUser;
    }

    public async Task<bool> CreateOrganisation(string name, string? description)
    {
        await organisationFacade.CreateOrganisation(name, description);
        return true;
    }

    public async Task<IEnumerable<OrganisationData>> ListOrganisations()
    {
        return await organisationFacade.ListOrganisations();
    }
}