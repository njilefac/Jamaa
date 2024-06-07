using System;

namespace Domain.Events
{
    public interface IEvent
    {
        Guid Id { get; }
        string Name { get; }
        DateTime Begin { get; }
        DateTime? End { get; }
    }
}
