using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Domain.Repositories;
using Domain.Values;
using Microsoft.Extensions.Logging;

namespace Libota.Application.Users.Services
{
    public class UserSessionService : IUserSessionService
    {
        private readonly IUserRepository _users;

        private readonly ILogger<UserSessionService> _logger;
        private static readonly UserSession? NullSession = new UserSession(false, "none");
        public Subject<UserSession?> CurrentSession { get; }

        public UserSessionService(ILogger<UserSessionService> logger, IUserRepository users)
        {
            _logger = logger;
            _users = users;
            CurrentSession = new Subject<UserSession?>();
        }

        public async Task<UserSession?> Authenticate(Credentials credentials)
        {
            if (credentials == null) throw new ArgumentNullException(nameof(credentials));

            _logger.LogInformation($"authenticating user...");
            var matchingUser = await _users.SingleOrDefault(x =>
                x.Account.Credentials.Equals(credentials));
            if (matchingUser == null){
                _logger.LogInformation($"authentication failed!");
                return NullSession;
            }
            _logger.LogInformation($"authenticated!");
            
            _logger.LogInformation($"creating user session...");
            var userSession = new UserSession(true, Guid.NewGuid().ToString());
            CurrentSession.OnNext(userSession);
            _logger.LogInformation($"user session created");

            return await Task.FromResult(userSession);
        }

        public async Task<bool> EndSession()
        {
            CurrentSession.OnNext(null);
            _logger.LogInformation($"user session terminated.");
            return await Task.FromResult(true);
        }
    }
}