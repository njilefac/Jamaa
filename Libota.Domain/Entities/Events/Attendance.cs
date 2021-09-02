using System;
using Domain.Entities.Members;

namespace Domain.Entities.Events
{
    public class Attendance
    {
        public Attendance(Guid id, Member member, IEvent @event)
        {
            Id = id;
            Member = member;
            Event = @event;
        }

        public Guid Id { get; }
        public Member Member { get; }
        public IEvent Event { get; }
    }
}
