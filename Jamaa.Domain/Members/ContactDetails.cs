using Domain.Shared;

namespace Domain.Members;

public sealed record ContactDetails(string Email, string PhoneNumber, Address Address)
{
    public static ContactDetails None { get; } = new(string.Empty, string.Empty, Address.None);
}