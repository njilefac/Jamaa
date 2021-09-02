using System.Threading.Tasks;
using Domain.Entities.Users;
using Domain.Repositories;
using Domain.Services;
using Microsoft.Extensions.Logging;

namespace Libota.Application
{
    public class UserManagementFacade : IUserManagementFacade
    {
        private readonly IUserSessionService _userSessionService;
        private readonly IUserRepository _users;
        private readonly ILogger<UserManagementFacade> _logger;
        public UserManagementFacade(IUserRepository users, 
            IUserSessionService userSessionService, 
            ILogger<UserManagementFacade> logger)
        {
            _users = users;
            _userSessionService = userSessionService;
            _logger = logger;
        }
        public async Task<User> GetSuperUser()
        {
            return await _users.SingleOrDefault(x => x.Account.IsSuperUser != null && 
                                                              x.Account.IsSuperUser.Value);
        }

        public async Task<User?> CreateSuperUser(string? username, string? email, string? password, string firstName,
            string lastName)
        {
            var newSuperUser = new User(username, password, email, firstName, string.Empty, lastName,
                true);
            return await _users.Add(newSuperUser);
        }
    }
}