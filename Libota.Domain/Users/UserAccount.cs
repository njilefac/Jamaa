using System;
using Domain.Values;

namespace Domain.Users
{
    public class UserAccount
    {
        public UserAccount(string? userName, string? password, string? email = null, bool? isSuperUser = false, bool? isActive = false)
        {
            Id = Guid.NewGuid();
            Credentials = new Credentials(userName, password);
            Email = email;
            IsActive = isActive;
            IsSuperUser = isSuperUser;
        }

        public Guid Id { get; }
        public string? Email { get; }
        public Credentials Credentials { get; }
        
        public DateTimeOffset CreatedOn { get; set; }
        
        public bool? IsSuperUser { get; }
        
        public bool? IsActive { get; }
    }
}
