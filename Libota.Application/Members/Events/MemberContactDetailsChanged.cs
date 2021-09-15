using EventFlow.Aggregates;
using Libota.Application.Members.Aggregates;

namespace Libota.Application.Members.Events
{
    public class MemberContactDetailsChanged : AggregateEvent<MemberAggregate, MemberId>
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