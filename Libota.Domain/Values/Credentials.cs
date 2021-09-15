using System;

namespace Domain.Values
{
    public class Credentials
    {
        public string? UserName { get; }
        public string? Password { get; }

        public Credentials(string? userName, string? password)
        {
            UserName = userName;
            Password = password;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not Credentials other)
                return false;
            return other.UserName == UserName && other.Password == Password;
        }

        protected bool Equals(Credentials other)
        {
            return UserName == other.UserName && Password == other.Password;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(UserName, Password);
        }
    }
}
