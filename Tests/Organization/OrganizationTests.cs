using System;
using Domain.Members;
using Domain.Organisation.Entities;
using Domain.Organisation.Values;
using Domain.Shared.Values;
using Shouldly;
using Xunit;

namespace UnitTests.Organization;

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
        result.FiscalCalendar.ShouldNotBeNull();
        result.FiscalCalendar.OrganisationId.Value.ShouldBe(result.Id);
        result.ChartOfAccounts.ShouldNotBeNull();
        result.ChartOfAccounts.OrganisationId.Value.ShouldBe(result.Id);
    }

    [Fact]
    public void ShouldRegisterNewMember()
    {
        // Arrange
        var firstName = "test first name";
        var middleName = "test middle name";
        var lastName = "test last name";
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