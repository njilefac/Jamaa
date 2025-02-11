using System;
using Domain.Shared.Values;

namespace Domain.Finances.Entities;

public class FinancialPeriod(Guid id, string name, DateTime begin, DateTime end) : ITimePeriod
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;
    public DateTime Begin { get; } = begin;

    public DateTime End { get; } = end;

    public override string ToString()
    {
        return $"{Name}: {Begin:MM/dd/yyyy} to {End:MM/dd/yyyy}";
    }
}