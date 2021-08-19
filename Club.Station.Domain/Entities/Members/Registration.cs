using System;
using Domain.Values;

namespace Domain.Entities.Members
{
    public class Registration
    {
        public Guid Id { get; }

        public DateTimeOffset Begin { get; }

        public DateTimeOffset? End { get; }

        public Member Member { get; }
        public MembershipType MembershipType { get; }
        public RegistrationStatus Status { get; private set; }

        public Registration(Member member, MembershipType membershipType, DateTime begin, DateTime? end)
        {
            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }
            Member = member;
            MembershipType = membershipType;
            Status = RegistrationStatus.Probation;
            Begin = begin;
            End = end;
        }
    }
}