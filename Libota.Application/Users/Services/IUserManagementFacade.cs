using Domain.Users;

namespace Libota.Application.Users.Services
{
    public interface IUserManagementFacade
    {
        Task<User> CreateUser(string username, string password, string email, string? firstName, string? lastName);
    }
}