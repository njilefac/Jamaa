using System;

namespace Domain.Values
{
    public class UserSession
    {
        public UserSession(bool isAuthenticated, string? userName, string? organisationId = "")
        {
            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(userName));
            IsAuthenticated = isAuthenticated;
            UserName = userName;
            OrganisationId = organisationId;
        }

        public bool IsAuthenticated { get; }

        public string? UserName { get; }
        public string? OrganisationId { get; }
    }
}