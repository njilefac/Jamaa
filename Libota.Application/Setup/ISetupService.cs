using System.Threading.Tasks;
using Domain.Entities.Users;

namespace Libota.Application.Setup
{
    public interface ISetupService
    {
        Task<User?> GetSuperUser();
        Task<User?> CreateSuperUser(string username, string password, string? email, string? firstName,
            string? lastName);
        Task<bool> CreateOrganization(string name, string description);
    }
}