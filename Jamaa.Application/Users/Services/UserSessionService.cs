using System.Reactive.Subjects;
using Domain.Security.Values;
using Domain.Users;
using Jamaa.Data.Models.Organisation;
using Microsoft.Extensions.Logging;

namespace Jamaa.Application.Users.Services;

public class UserSessionService(ILogger<UserSessionService> logger, IUserRepository users) : IUserSessionService
{
    private static readonly UserSession? NullSession = new(false, "none", null, null);
    private readonly Subject<UserSession?> _userSessions = new();
    public IObservable<UserSession?> UserSessions => _userSessions;
    public UserSession? CurrentUserSession { get; private set; }

    public async Task<UserSession?> Authenticate(Credentials credentials, OrganisationData? organisation)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        logger.LogDebug("authenticating user...");
        var matchingUser = await users.SingleOrDefault(x =>
            x.Account.Credentials.Equals(credentials));
        if (matchingUser == null)
        {
            logger.LogDebug("authentication failed!");
            return NullSession;
        }

        logger.LogDebug("authenticated!");

        logger.LogDebug("creating user session...");
        var userSession = new UserSession(true, credentials.UserName, matchingUser.Id, organisation);
        CurrentUserSession = userSession;
        _userSessions.OnNext(userSession);
        logger.LogDebug("user session created");

        return await Task.FromResult(userSession);
    }

    public async Task<bool> EndSession()
    {
        CurrentUserSession = null;
        _userSessions.OnNext(null);
        logger.LogDebug("user session terminated.");
        return await Task.FromResult(true);
    }
}