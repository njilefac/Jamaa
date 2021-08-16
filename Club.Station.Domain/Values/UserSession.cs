using System;

namespace Domain.Values
{
    public class UserSession
    {
        public UserSession(bool authenticated, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(token));
            Authenticated = authenticated;
            Token = token;
        }
        public bool Authenticated { get; }
        public string Token { get; }

        //todo: value should be decoded from token
        public DateTimeOffset ExpiresAt => DateTimeOffset.Now.AddMinutes(30);
    }
}