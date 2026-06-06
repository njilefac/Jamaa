using System;
using Domain.Shared.Values;
using Domain.Users;

namespace Jamaa.Data.Models.Users;

public class UserData
{
    public Guid Id { get; set; }
    public required string UserName { get; set; }
    public required string Password { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string MiddleName { get; set; }
    public required string LastName { get; set; }
    public Gender Gender { get; set; }
    public bool IsActive { get; set; }
    public bool IsSuperUser { get; set; }

    public string? DashboardLayout { get; set; }


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
            data.IsActive,
            data.DashboardLayout,
            data.Id
        );
    }

    public static UserData Map(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserData
        {
            Id = user.Account.Id,
            UserName = user.Account.Credentials.UserName ?? string.Empty,
            Password = user.Account.Credentials.Password ?? string.Empty,
            Email = user.Account.Email,
            FirstName = user.FirstName ?? string.Empty,
            MiddleName = user.MiddleName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            IsActive = user.Account.IsActive ?? false,
            IsSuperUser = user.Account.IsSuperUser ?? false,
            DashboardLayout = user.DashboardLayout
        };
    }
}