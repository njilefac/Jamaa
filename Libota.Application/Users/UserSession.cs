using System;
using Libota.Application.Organisation.Queries.Models;

namespace Libota.Application.Users
{
    public class UserSession
    {
        public UserSession(bool isAuthenticated, string? userName, OrganisationReadModel? organisation)
        {
            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(userName));
            IsAuthenticated = isAuthenticated;
            UserName = userName;
            Organisation = organisation;
        }

        public bool IsAuthenticated { get; }

        public string? UserName { get; }
        public OrganisationReadModel? Organisation { get; }
    }
}