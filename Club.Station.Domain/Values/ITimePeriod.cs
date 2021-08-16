using System;

namespace Domain.Values
{
    public interface ITimePeriod
    {
        DateTime Begin { get; }

        DateTime End { get; }
    }
}
