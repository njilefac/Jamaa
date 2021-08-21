using System;

namespace Domain.Values
{
    public class UserSession
    {
        public UserSession(bool isAuthenticated, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(token));
            IsAuthenticated = isAuthenticated;
            Token = token;
        }
        public bool IsAuthenticated { get; }
        public string Token { get; }

        //todo: value should be decoded from token
        public DateTimeOffset ExpiresAt => DateTimeOffset.Now.AddMinutes(30);
    }
}