using System.Threading.Tasks;
using Domain.Users;
using Microsoft.Extensions.Logging;

namespace Libota.Application.Users.Services
{
    public class UserManagementFacade(IUserRepository users, ILogger<UserManagementFacade> logger) : IUserManagementFacade
    {
        public async Task<User?> CreateUser(string username, string password, string? email, string? firstName, string? lastName)
        {
            logger.LogInformation($"creating user account...");
            var newUser = new User(username, password, email, firstName, null, lastName, isSuperUser: false);
            var user = await users.Add(newUser);
            logger.LogInformation($"user account created!");
            
            return user;
        }
    }
}