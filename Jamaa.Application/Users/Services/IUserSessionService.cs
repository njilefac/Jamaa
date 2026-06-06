using Domain.Security.Values;
using Jamaa.Data.Models.Organisation;

namespace Jamaa.Application.Users.Services;

public interface IUserSessionService
{
    IObservable<UserSession?> UserSessions { get; }

    UserSession? CurrentUserSession { get; }
    Task<UserSession?> Authenticate(Credentials credentials, OrganisationData? organisation);
    Task<bool> EndSession();
}