using System;
using Domain.Values;

namespace Domain.Entities.Finances
{
    public interface IFee
    {
        Guid Id { get; }
        string Name { get; }
        string Description { get; }
        MoneyAmount Amount { get; }
    }
}
