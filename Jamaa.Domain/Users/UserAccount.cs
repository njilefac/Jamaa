using System;
using Domain.Security.Values;

namespace Domain.Users;

public record UserAccount(
    string UserName,
    string Password,
    string Email,
    bool? IsSuperUser = false,
    bool? IsActive = false,
    Guid? ExternalId = null)
{
    public Guid Id { get; } = ExternalId ?? Guid.NewGuid();
    public Credentials Credentials => new(UserName, Password);
    public DateTimeOffset CreatedOn { get; set; }
}