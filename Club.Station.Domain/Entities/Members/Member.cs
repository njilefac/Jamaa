using Domain.Entities.Shared;

namespace Domain.Entities.Members
{
    public class Member : Person
    {
        public Member()
        {
        }

        public ContactDetails ContactDetails { get; private set; }
    }
}
