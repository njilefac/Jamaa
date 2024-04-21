using System;
using Domain.Values;

namespace Libota.Data.Models.Users
{
    public class UserData
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public Gender Gender { get; set; }
        public bool IsActive { get; set; }
        public bool IsSuperUser { get; set; }
    }
}