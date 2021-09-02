using System;
using Domain.Values;

namespace Domain.Entities.Shared
{
    public abstract class Person
    {
        protected Person(string? firstName, string? middleName, string? lastName, Gender gender = Gender.Unknown, string? title = null, DateTime? dateOfBirth = null)
        {
            FirstName = firstName;
            MiddleName = middleName;
            LastName = lastName;
            Title = title;
            Gender = gender;
            DateOfBirth = dateOfBirth;
        }

        public string? Title { get; }
        public string? FirstName { get; }
        public string? MiddleName { get; }
        public string? LastName { get; }
        public Gender Gender { get; }
        public DateTime? DateOfBirth { get; }
        
    }
}