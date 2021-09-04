using System.Threading.Tasks;
using Domain.Entities.Users;

namespace Libota.Application.Users
{
    public interface IUserManagementFacade
    {
        Task<User?> CreateUser(string username, string password, string? email, string? firstName, string? lastName);
    }
}