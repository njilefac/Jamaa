using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Users;
using Libota.Application.Organisation;
using Libota.Application.Users.Services;
using Libota.Data.Models.Organisation;
using Microsoft.Extensions.Logging;

namespace Libota.Application.Setup
{
    public class SetupService : ISetupService
    {
        private readonly IUserRepository _users;
        private readonly ILogger<UserManagementFacade> _logger;
        private readonly IOrganisationManagementFacade _organisationManagementFacade;

        public SetupService(
            IUserRepository users,
            IOrganisationManagementFacade organisationManagementFacade,
            ILogger<UserManagementFacade> logger)
        {
            _users = users;
            _logger = logger;
            _organisationManagementFacade = organisationManagementFacade;
        }

        public async Task<User?> GetSuperUser()
        {
            return await _users.SingleOrDefault(x => x.Account.IsSuperUser != null &&
                                                     x.Account.IsSuperUser.Value);
        }

        public async Task<User?> CreateSuperUser(string username, string password, string? email, string? firstName,
            string? lastName)
        {
            _logger.LogInformation($"creating super user account...");
            var newSuperUser = new User(username, password, email, firstName, string.Empty, lastName,
                isSuperUser: true);
            var superUser = await _users.Add(newSuperUser);
            _logger.LogInformation($"super user account created!");
            return superUser;
        }

        public async Task<bool> CreateOrganisation(string name, string? description)
        {
            await _organisationManagementFacade.CreateOrganisation(name, description);
            return true;
        }

        public async Task<IEnumerable<OrganisationReadModel>> ListOrganisations()
        {
            return await _organisationManagementFacade.ListOrganisations();
        }
    }
}