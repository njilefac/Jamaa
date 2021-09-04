using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Users;
using Domain.Repositories;
using EventFlow;
using EventFlow.Queries;
using Libota.Application.Organisation;
using Libota.Application.Organisation.Commands;
using Libota.Application.Users;
using Microsoft.Extensions.Logging;

namespace Libota.Application.Setup
{
    public class SetupService : ISetupService
    {
        private readonly IUserRepository _users;
        private readonly ILogger<UserManagementFacade> _logger;
        private readonly ICommandBus _commandBus;
        private readonly IQueryProcessor _queryProcessor;

        public SetupService(
            IUserRepository users,
            ILogger<UserManagementFacade> logger, 
            ICommandBus commandBus, 
            IQueryProcessor queryProcessor)
        {
            _users = users;
            _logger = logger;
            _commandBus = commandBus;
            _queryProcessor = queryProcessor;
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

        public async Task<bool> CreateOrganization(string name, string description)
        {
            var result = await _commandBus.PublishAsync(
                new CreateOrganisationCommand(new OrganisationIdentity(Guid.NewGuid()), name, description),
                CancellationToken.None);
            
            return result.IsSuccess;
        }
    }
}