using System;

namespace Domain.Accounting.Values;

public record FiscalCalendarId(string Value)
{
    public static FiscalCalendarId New()
    {
        return new FiscalCalendarId(Guid.NewGuid().ToString());
    }

    public static FiscalCalendarId With(string value)
    {
        return new FiscalCalendarId(value);
    }

    public static FiscalCalendarId With(Guid guid)
    {
        return new FiscalCalendarId(guid.ToString());
    }
}