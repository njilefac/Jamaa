using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Domain.Values;
using Microsoft.Extensions.Logging;

namespace Domain.Services
{
    public class UserSessionService : IUserSessionService
    {
        private readonly Dictionary<string, string> _knownCredentials = new()
        {
            ["admin"] = "Test123#",
        };

        private readonly ILogger<UserSessionService> _logger;
        public Subject<UserSession?> CurrentSession { get; }
        public UserSessionService(ILogger<UserSessionService> logger)
        {
            _logger = logger;
            CurrentSession = new Subject<UserSession?>();
        }
        public async Task<UserSession?> Authenticate(Credentials credentials)
        {
            _logger.LogInformation($"authenticating user...");
            var nullSession = new UserSession(false, "none");
            if (credentials.UserName != null && !_knownCredentials.ContainsKey(credentials.UserName))
            {
                CurrentSession.OnNext(nullSession);
                _logger.LogInformation($"authentication failed!");
                return await Task.FromResult(nullSession);
            }

            if (credentials.UserName != null && !_knownCredentials[credentials.UserName].Equals(credentials.Password))
            {
                CurrentSession.OnNext(nullSession);
                _logger.LogInformation($"authentication failed!");
                return await Task.FromResult(nullSession);
            }
                
            var userSession = new UserSession(true, Guid.NewGuid().ToString());
            CurrentSession.OnNext(userSession);
            _logger.LogInformation($"authenticated!");
            _logger.LogInformation($"created user session");
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