using Domain.Security.Values;
using Jamaa.Data.Models.Organisation;

namespace Jamaa.Application.Users.Services
{
    public interface IUserSessionService
    {
        Task<UserSession?> Authenticate(Credentials credentials, OrganisationData? organisation);
        Task<bool> EndSession();
        IObservable<UserSession?> UserSessions { get; }
        
        UserSession? CurrentUserSession { get; }
    }
}