namespace Domain.Entities
{
    public class Member : Person
    {
        public Member()
        {
        }

        public ContactDetails ContactDetails { get; private set; }
    }
}
