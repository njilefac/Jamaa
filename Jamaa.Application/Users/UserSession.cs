using Jamaa.Data.Models.Organisation;

namespace Jamaa.Application.Users
{
    public class UserSession
    {
        public UserSession(bool isAuthenticated, string? userName, OrganisationData? organisation)
        {
            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentException(@"Value cannot be null or whitespace.", nameof(userName));
            IsAuthenticated = isAuthenticated;
            UserName = userName;
            Organisation = organisation;
        }

        public bool IsAuthenticated { get; }

        public string? UserName { get; }
        public OrganisationData? Organisation { get; }
    }
}