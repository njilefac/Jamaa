using System;
using Domain.Members;
using Domain.Organisation.Entities;
using Domain.Organisation.Values;
using Domain.Shared.Values;
using Shouldly;
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
            var result = new Organisation(name, description);

            // Assert
            result.Name.ShouldBe(name);
            result.Description.ShouldBe(description);
        }

        [Fact]
        public void ShouldRegisterNewMember()
        {
            // Arrange
            string firstName = "test first name";
            string middleName = "test middle name";
            string lastName = "test last name";
            var member = new Member(firstName, middleName, lastName, Gender.Female, DateTime.Today);

            var organization = new Organisation("Unity Club", "Test organization");

            // Act
            var registration = organization.Register(member, MembershipType.Regular, DateTime.Today);

            // Assert
            registration.ShouldNotBeNull();
            registration.Member.ShouldBe(member);
            registration.Status.ShouldBe(RegistrationStatus.Probation);
            registration.Begin.ShouldBe(DateTime.Today);
            registration.End.ShouldBeNull();
        }
    }
}
