using System;
using Domain.Members;
using Domain.Shared;
using Shouldly;
using Xunit;

namespace UnitTests.Members;

public class ContactDetailsTests
{
    [Fact]
    public void ShouldExposeAnEmptyNoneInstance()
    {
        ContactDetails.None.Email.ShouldBe(string.Empty);
        ContactDetails.None.PhoneNumber.ShouldBe(string.Empty);
        ContactDetails.None.Address.ShouldBe(Address.None);
    }

    [Fact]
    public void ShouldBehaveAsAValueObjectWhenDataMatches()
    {
        var address = new Address(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Kigali",
            "KN 1 Ave",
            "12",
            "00000",
            "RW");

        var left = new ContactDetails("member@example.com", "+250700000000", address);
        var right = new ContactDetails("member@example.com", "+250700000000", address);

        left.ShouldBe(right);
    }
}