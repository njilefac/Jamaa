using System;
using Domain.Members;

namespace Domain.Events;

public record Attendance(Guid Id, Member Member, IEvent Event)
{
    public Guid Id { get; } = Id;
    public Member Member { get; } = Member;
    public IEvent Event { get; } = Event;
}