using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Domain.Values;

namespace Domain.Services
{
    public class UserSessionService : IUserSessionService
    {
        private readonly Dictionary<string, string> _knownCredentials = new()
        {
            ["admin"] = "Test123#",
        };
        
        public Subject<UserSession> CurrentSession { get; }

        public UserSessionService()
        {
            CurrentSession = new Subject<UserSession>();
        }
        
        public async Task<UserSession> Authenticate(Credentials credentials)
        {
            var nullSession = new UserSession(false, "none");
            if (!_knownCredentials.ContainsKey(credentials.UserName))
            {
                CurrentSession.OnNext(nullSession);
                return await Task.FromResult(nullSession);
            }

            if (!_knownCredentials[credentials.UserName].Equals(credentials.Password))
            {
                CurrentSession.OnNext(nullSession);
                return await Task.FromResult(nullSession);
            }
                
            var userSession = new UserSession(true, Guid.NewGuid().ToString());
            CurrentSession.OnNext(userSession);
            return await Task.FromResult(userSession);
        }

        public async Task<bool> EndSession(UserSession session)
        {
            CurrentSession.OnNext(null);
            return await Task.FromResult(true);
        }
    }
}