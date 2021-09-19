using System;
using System.ComponentModel.DataAnnotations;
using Domain.Values;
using Libota.Application.Organisation.Queries.Models;

namespace Libota.Application.Members.Queries.Models
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