using System;
using Domain.Shared.Values;
using Domain.Users;

namespace Libota.Data.Models.Users;

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


    public static User Map(UserData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return new User(
            data.UserName,
            data.Password,
            data.Email,
            data.FirstName,
            data.MiddleName,
            data.LastName,
            data.IsSuperUser,
            data.IsActive
        );
    }

    public static UserData Map(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserData
        {
            Id = user.Account.Id,
            UserName = user.Account.Credentials.UserName,
            Password = user.Account.Credentials.Password,
            Email = user.Account.Email,
            FirstName = user.FirstName,
            MiddleName = user.MiddleName,
            LastName = user.LastName
        };
    }
}