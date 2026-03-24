using Domain.Shared;

namespace Domain.Users;

public class User(
    string userName,
    string password,
    string email,
    string? firstName,
    string? middleName,
    string? lastName,
    bool isSuperUser = false,
    bool isActive = true)
    : Person(firstName, middleName, lastName)
{
    public UserAccount Account =>  new(userName, password, email, isSuperUser, isActive);
}