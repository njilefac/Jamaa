using System;
using Domain.Values;

namespace Domain.Entities.Shared
{
    public abstract class Person
    {
        public string Title { get; private set; }
        public string FirstName { get; private set; }
        public string MiddleName { get; private set; }
        public string LastName { get; private set; }
        public Gender Gender { get; private set; }
        public DateTime? DateOfBirth { get; }
    }
}