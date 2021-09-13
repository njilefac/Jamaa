using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Domain.Values;

namespace Libota.Application.Users.Services
{
    public interface IUserSessionService
    {
        Task<UserSession?> Authenticate(Credentials credentials, string organisationId = "");
        Task<bool> EndSession();
        Subject<UserSession?> UserSessions { get; }
        
        UserSession? CurrentUserSession { get; }
    }
}