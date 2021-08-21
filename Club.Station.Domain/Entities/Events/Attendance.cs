using System;
using Domain.Entities.Members;

namespace Domain.Entities.Events
{
    public class Attendance
    {
        public Guid Id { get; private set; }
        public Member Member { get; private set; }
        public IEvent Event { get; private set; }
    }
}
