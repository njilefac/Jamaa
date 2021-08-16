using System;

namespace Domain.Entities
{
    public class Attendance
    {
        public Guid Id { get; private set; }
        public Member Member { get; private set; }
        public IEvent Event { get; private set; }
    }
}
