using System.Reactive.Subjects;
using System.Threading.Tasks;
using Domain.Values;
using Libota.Application.Organisation;
using Libota.Application.Organisation.Queries.Models;
using Libota.Application.Security;

namespace Libota.Application.Users.Services
{
    public interface IUserSessionService
    {
        Task<UserSession?> Authenticate(Credentials credentials, OrganisationReadModel? organisation);
        Task<bool> EndSession();
        Subject<UserSession?> UserSessions { get; }
        
        UserSession? CurrentUserSession { get; }
    }
}