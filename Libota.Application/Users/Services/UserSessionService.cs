using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Domain.Security.Values;
using Domain.Users;
using Libota.Data.Models.Organisation;
using Microsoft.Extensions.Logging;

namespace Libota.Application.Users.Services
{
    public class UserSessionService(ILogger<UserSessionService> logger, IUserRepository users) : IUserSessionService
    {
        private static readonly UserSession? NullSession = new(false, "none", null);
        private readonly Subject<UserSession?> _userSessions = new();
        public IObservable<UserSession?> UserSessions => _userSessions;
        public UserSession? CurrentUserSession { get; private set; }

        public async Task<UserSession?> Authenticate(Credentials credentials, OrganisationData? organisation)
        {
            ArgumentNullException.ThrowIfNull(credentials);

            logger.LogInformation("authenticating user...");
            var matchingUser = await users.SingleOrDefault(x =>
                x.Account.Credentials.Equals(credentials));
            if (matchingUser == null)
            {
                logger.LogInformation("authentication failed!");
                return NullSession;
            }

            logger.LogInformation("authenticated!");

            logger.LogInformation("creating user session...");
            var userSession = new UserSession(true, credentials.UserName, organisation);
            _userSessions.OnNext(userSession);
            CurrentUserSession = userSession;
            logger.LogInformation("user session created");

            return await Task.FromResult(userSession);
        }

        public async Task<bool> EndSession()
        {
            _userSessions.OnNext(null);
            CurrentUserSession = null;
            logger.LogInformation("user session terminated.");
            return await Task.FromResult(true);
        }
    }
}