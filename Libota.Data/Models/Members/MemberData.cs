using System.ComponentModel.DataAnnotations;
using Domain.Shared.Values;
using Libota.Data.Models.Organisation;

namespace Libota.Data.Models.Members
{
    public class MemberData
    {
        [Key] public string Id { get; set; }
        public string LastName { get; set; }
        public string? MiddleName { get; set; }
        public string FirstName { get; set; }
        public Gender Gender { get; set; }
        public OrganisationData Organisation { get; set; }
        public string OrganisationId { get; set; }
        public RegistrationData Registration { get; set; }
    }
}