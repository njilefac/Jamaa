using System.Threading.Tasks;
using Domain.Entities.Users;
using Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Libota.Application.Users.Services
{
    public class UserManagementFacade : IUserManagementFacade
    {
        private readonly IUserRepository _users;
        private readonly ILogger<UserManagementFacade> _logger;
        public UserManagementFacade(IUserRepository users, 
            ILogger<UserManagementFacade> logger)
        {
            _users = users;
            _logger = logger;
        }
        public async Task<User?> CreateUser(string username, string password, string? email, string? firstName, string? lastName)
        {
            _logger.LogInformation($"creating user account...");
            User newUser = new User(username, password, email, firstName, null, lastName, isSuperUser: false);
            var user = await _users.Add(newUser);
            _logger.LogInformation($"user account created!");
            
            return user;
        }
    }
}