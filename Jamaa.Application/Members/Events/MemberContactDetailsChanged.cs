using Jamaa.Application.Members.Aggregates;

namespace Jamaa.Application.Members.Events;

public record MemberContactDetailsChanged(
    MemberId MemberId,
    string Email,
    string PhoneNumber,
    string Street,
    string HouseNumber,
    string City,
    string PostCode,
    string CountryCode);