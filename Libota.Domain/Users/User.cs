using Domain.Shared;

namespace Domain.Users;

public class User : Person
{
    public UserAccount Account { get; }


    public User(string? userName, string? password, string? email, string? firstName, string? middleName,
        string? lastName, bool isSuperUser = false, bool isActive = true)
        : base(firstName, middleName, lastName)
    {
        Account = new UserAccount(userName, password, email, isSuperUser, isActive);
    }
}