using System;

namespace Domain.Accounting.Values;

public record ChartOfAccountsId(string Value)
{
    public static ChartOfAccountsId New()
    {
        return new ChartOfAccountsId(Guid.NewGuid().ToString());
    }

    public static ChartOfAccountsId With(string value)
    {
        return new ChartOfAccountsId(value);
    }

    public static ChartOfAccountsId With(Guid guid)
    {
        return new ChartOfAccountsId(guid.ToString());
    }
}