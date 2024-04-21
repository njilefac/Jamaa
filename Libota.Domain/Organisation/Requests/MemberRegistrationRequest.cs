using System;
using Domain.Organisation.Values;
using Domain.Values;

namespace Domain.Organisation.Requests
{
    public class MemberRegistrationRequest
    {
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public Gender Gender { get; set; }
        public DateTime? RegistrationBegin { get; set; }
        public MembershipType MembershipType { get; set; }
        public OrganisationId? OrganisationId { get; set; }
    }
}