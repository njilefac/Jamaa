namespace Libota.Application.Members.Events
{
    public record MemberContactDetailsChanged
    {
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Street { get; set; }
        public string HouseNumber { get; set; }
        public string City { get; set; }
        public string PostCode { get; set; }
        public string CountryCode { get; set; }
    }
}