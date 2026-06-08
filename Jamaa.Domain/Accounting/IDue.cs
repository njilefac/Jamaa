using System;
using Domain.Accounting.Values;

namespace Domain.Accounting;

public interface IDue
{
    Guid Id { get; }
    string Name { get; }
    string Description { get; }
    MoneyAmount Amount { get; }
}