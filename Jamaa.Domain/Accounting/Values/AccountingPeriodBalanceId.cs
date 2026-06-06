using System;

namespace Domain.Accounting.Values;

public record AccountingPeriodBalanceId(string Value)
{
    public static AccountingPeriodBalanceId New() => new(Guid.NewGuid().ToString());
    
    public static AccountingPeriodBalanceId With(string value) => new(value);
}
