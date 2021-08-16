using System;

namespace Domain.Entities
{
    public interface IEvent
    {
        Guid Id { get; }
        string Name { get; }
        DateTime Begin { get; }
        DateTime? End { get; }
    }
}
