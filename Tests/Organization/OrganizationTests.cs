using System;
using Domain.Entities.Members;
using Domain.Values;
using FluentAssertions;
using Xunit;

namespace UnitTests.Organization
{
    public class OrganizationTests
    {
        [Fact]
        public void ShouldCreateOrganization()
        {
            //Arrange
            var name = "Test Organization";
            var description = "this is a test organization";

            // Act
            var result = new Domain.Entities.Shared.Organisation(name, description);

            // Assert
            result.Name.Should().Be(name);
            result.Description.Should().Be(description);
        }

        [Fact]
        public void ShouldRegisterNewMember()
        {
            // Arrange
            string firstName = "test first name";
            string middleName = "test middle name";
            string lastName = "test last name";
            var member = new Member(firstName, middleName, lastName, Gender.Female, DateTime.Today);

            var organization = new Domain.Entities.Shared.Organisation("Unity Club", "Test organization");

            // Act
            var registration = organization.Register(member, MembershipType.Regular, DateTime.Today);

            // Assert
            registration.Should().NotBeNull("a registration should be created");
            registration.Member.Should().Be(member);
            registration.Status.Should().Be(RegistrationStatus.Probation, "all newly registered members are on probabation by default");
            registration.Begin.Should().Be(DateTime.Today);
            registration.End.Should().BeNull("registration should not be time-limited");
        }
    }
}
