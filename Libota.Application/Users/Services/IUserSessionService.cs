using Domain.Security.Values;
using Libota.Data.Models.Organisation;

namespace Libota.Application.Users.Services
{
    public interface IUserSessionService
    {
        Task<UserSession?> Authenticate(Credentials credentials, OrganisationData? organisation);
        Task<bool> EndSession();
        IObservable<UserSession?> UserSessions { get; }
        
        UserSession? CurrentUserSession { get; }
    }
}