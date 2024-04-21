using System;
using System.ComponentModel.DataAnnotations;
using Domain.Organisation.Values;
using Libota.Data.Models.Organisation;

namespace Libota.Data.Models.Members
{
    public class Registration
    {
        [Key] public string Id { get; set; }
        public Member Member { get; set; }
        public string MemberId { get; set; }
        public DateTime StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }
        public MembershipType MembershipType { get; set; }

        public RegistrationStatus Status { get; set; }
        public OrganisationReadModel Organisation { get; set; }
    }
}