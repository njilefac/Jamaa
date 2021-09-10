using System;

namespace Domain.Values
{
    public class UserSession
    {
        public UserSession(bool isAuthenticated, string? userName, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(token));
            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(userName));
            IsAuthenticated = isAuthenticated;
            Token = token;
            UserName = userName;
        }
        public bool IsAuthenticated { get; }

        public string? UserName { get; }
        public string Token { get; }

        //todo: value should be decoded from token
        public DateTimeOffset ExpiresAt => DateTimeOffset.Now.AddMinutes(30);
    }
}