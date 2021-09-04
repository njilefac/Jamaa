using System.Reactive.Subjects;
using System.Threading.Tasks;
using Domain.Values;

namespace Libota.Application.Users.Services
{
    public interface IUserSessionService
    {
        Task<UserSession?> Authenticate(Credentials credentials);
        Task<bool> EndSession();
        Subject<UserSession?> CurrentSession { get; }
    }
}