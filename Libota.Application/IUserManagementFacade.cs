using System.Threading.Tasks;
using Domain.Entities.Users;

namespace Libota.Application
{
    public interface IUserManagementFacade
    {
        Task<User> GetSuperUser();

        Task<User?> CreateSuperUser(string? username, string? email, string? password, string firstName,
            string lastName);
    }
}