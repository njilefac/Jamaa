using Domain.Entities.Shared;

namespace Domain.Entities.Users
{
    public class User : Person
    {
        public UserAccount Account { get; }
    }
}