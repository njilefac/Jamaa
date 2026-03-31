using System;
using System.ComponentModel.DataAnnotations;
using Domain.Organisation.Values;
using Jamaa.Data.Models.Organisation;

namespace Jamaa.Data.Models.Members
{
    public class RegistrationData
    {
        [Key] public string Id { get; set; }
        public MemberData Member { get; set; }
        public string MemberId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public MembershipType MembershipType { get; set; }
        public RegistrationStatus Status { get; set; }
        public OrganisationData Organisation { get; set; }
    }
}