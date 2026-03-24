using System;

namespace Domain.Shared.Values;

public interface ITimePeriod
{
    DateTime Begin { get; }

    DateTime End { get; }
}