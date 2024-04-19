using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Domain.Users;
using Domain.Values;
using Libota.Application.Organisation;
using Libota.Application.Organisation.Queries.Models;
using Libota.Application.Security;
using Microsoft.Extensions.Logging;

namespace Libota.Application.Users.Services
{
    public class UserSessionService : IUserSessionService
    {
        private readonly IUserRepository _users;

        private readonly ILogger<UserSessionService> _logger;
        private static readonly UserSession? NullSession = new UserSession(false, "none", null);
        public Subject<UserSession?> UserSessions { get; }
        public UserSession? CurrentUserSession { get; private set; }

        public UserSessionService(ILogger<UserSessionService> logger, IUserRepository users)
        {
            _logger = logger;
            _users = users;
            UserSessions = new Subject<UserSession?>();
        }

        public async Task<UserSession?> Authenticate(Credentials credentials,
            OrganisationReadModel? organisation)
        {
            if (credentials == null) throw new ArgumentNullException(nameof(credentials));

            _logger.LogInformation($"authenticating user...");
            var matchingUser = await _users.SingleOrDefault(x =>
                x.Account.Credentials.Equals(credentials));
            if (matchingUser == null)
            {
                _logger.LogInformation($"authentication failed!");
                return NullSession;
            }

            _logger.LogInformation($"authenticated!");

            _logger.LogInformation($"creating user session...");
            var userSession = new UserSession(true, credentials.UserName, organisation);
            UserSessions.OnNext(userSession);
            CurrentUserSession = userSession;
            _logger.LogInformation($"user session created");

            return await Task.FromResult(userSession);
        }

        public async Task<bool> EndSession()
        {
            UserSessions.OnNext(null);
            CurrentUserSession = null;
            _logger.LogInformation($"user session terminated.");
            return await Task.FromResult(true);
        }
    }
}