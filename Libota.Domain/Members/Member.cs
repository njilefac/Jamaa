using System;
using Domain.Shared;
using Domain.Shared.Values;

namespace Domain.Members
{
    public class Member : Person
    {
        public Member(string? firstName, string? middleName, string? lastName, Gender gender, DateTime? dateOfBirth)
        :base(firstName, middleName, lastName, gender, dateOfBirth: dateOfBirth)
        {
        }

        public ContactDetails ContactDetails => ContactDetails.None;
    }
}
