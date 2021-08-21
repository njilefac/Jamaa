using System;
using Domain.Values;

namespace Domain.Entities.Users
{
    public class UserAccount
    {
        public Guid Id { get; }
        public string Email { get; set; }
        public Credentials Credentials { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public bool IsActive { get; set; }
    }
}
