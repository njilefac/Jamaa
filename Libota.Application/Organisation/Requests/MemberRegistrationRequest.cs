using System;
using Domain.Values;
using Libota.Application.Organisation.Aggregates;

namespace Libota.Application.Organisation.Requests
{
    public class MemberRegistrationRequest
    {
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public Gender Gender { get; set; }
        public DateTimeOffset RegistrationBegin { get; set; }
        public MembershipType MembershipType { get; set; }

        public OrganisationId OrganisationId { get; set; }
    }
}