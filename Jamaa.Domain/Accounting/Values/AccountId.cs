using System;

namespace Domain.Accounting.Values;

public record AccountId(string Value)
{
    public static AccountId With(Guid guid)
    {
        return new AccountId(guid.ToString());
    }

    public static AccountId With(string value)
    {
        return new AccountId(value);
    }
}