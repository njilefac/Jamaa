namespace Domain.Entities
{
    using System;

    using Domain.Values;

    public interface IFee
    {
        Guid Id { get; }
        string Name { get; }
        string Description { get; }
        MoneyAmount Amount { get; }
    }
}
