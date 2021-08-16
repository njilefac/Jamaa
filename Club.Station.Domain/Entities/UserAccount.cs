namespace Domain.Entities
{
    using System;

    using Domain.Values;

    public class UserAccount
    {
        public Guid Id { get; private set; }
        public string Email { get; set; }
        public Credentials Credentials { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
    }
}
