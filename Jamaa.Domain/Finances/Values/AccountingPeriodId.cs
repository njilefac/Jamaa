using System;

namespace Domain.Finances.Values;

public record AccountingPeriodId(string Value)
{
    public static AccountingPeriodId New()
    {
        return new AccountingPeriodId(Guid.NewGuid().ToString());
    }

    public static AccountingPeriodId With(string value)
    {
        return new AccountingPeriodId(value);
    }
}

