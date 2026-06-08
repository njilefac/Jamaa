using System;

namespace Domain.Accounting.Values;

public record FiscalYearId(string Value)
{
    public static FiscalYearId New()
    {
        return new FiscalYearId(Guid.NewGuid().ToString());
    }

    public static FiscalYearId With(string value)
    {
        return new FiscalYearId(value);
    }
}