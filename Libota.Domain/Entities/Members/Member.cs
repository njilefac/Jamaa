using System;
using Domain.Entities.Shared;
using Domain.Values;

namespace Domain.Entities.Members
{
    public class Member : Person
    {
        public Member(string firstName, string? middleName, string lastName, Gender gender, DateTime? dateOfBirth)
        :base(firstName, middleName, lastName, gender, dateOfBirth: dateOfBirth)
        {
        }

        public ContactDetails ContactDetails => ContactDetails.None;
    }
}
