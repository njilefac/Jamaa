using System;
using Domain.Finances.Values;

namespace Domain.Finances;

public interface IFee
{
    Guid Id { get; }
    string Name { get; }
    string Description { get; }
    MoneyAmount Amount { get; }
}