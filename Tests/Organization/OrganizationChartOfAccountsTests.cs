using System.Collections.Generic;
using Domain.Accounting.Entities;
using Domain.Accounting.Values;
using Domain.Organisation.Entities;
using Domain.Organisation.Values;
using Shouldly;
using Xunit;

namespace UnitTests.Organization;

public class OrganizationChartOfAccountsTests
{
    [Fact]
    public void ShouldStoreAccountsInOrganizationChartOfAccounts()
    {
        // Arrange
        var organisation = new Organisation("Unity Club", "Accounting test organization");
        var organisationId = OrganisationId.With(organisation.Id);

        var parent = new Account(
            AccountId.With("1000"),
            organisationId,
            "1000",
            "Assets",
            AccountType.Asset,
            "Root assets account");

        var child = new Account(
            AccountId.With("1100"),
            organisationId,
            "1100",
            "Cash",
            AccountType.Asset,
            "Cash on hand",
            parent.Id);

        // Act
        organisation.ChartOfAccounts.Accounts.Add(parent);
        organisation.ChartOfAccounts.Accounts.Add(child);

        // Assert
        organisation.ChartOfAccounts.Accounts.Count.ShouldBe(2);
    }

    [Fact]
    public void ShouldRemoveAccountFromOrganizationChartOfAccountsCollection()
    {
        // Arrange
        var organisation = new Organisation("Unity Club", "Accounting test organization");
        var organisationId = OrganisationId.With(organisation.Id);

        var parent = new Account(AccountId.With("2000"), organisationId, "2000", "Liabilities", AccountType.Liability);
        var child = new Account(AccountId.With("2100"), organisationId, "2100", "Payables", AccountType.Liability,
            parentId: parent.Id);

        organisation.ChartOfAccounts.Accounts.Add(parent);
        organisation.ChartOfAccounts.Accounts.Add(child);

        // Act
        var wasRemoved = organisation.ChartOfAccounts.Accounts.Remove(parent);

        // Assert
        wasRemoved.ShouldBeTrue();
        organisation.ChartOfAccounts.Accounts.Count.ShouldBe(1);
        organisation.ChartOfAccounts.Accounts.ShouldContain(child);
    }

    [Fact]
    public void ShouldAllowLinkingSubAccountInDomainAccountModel()
    {
        // Arrange
        var organisation = new Organisation("Unity Club", "Accounting test organization");
        var organisationId = OrganisationId.With(organisation.Id);

        var parentId = AccountId.With("3000");
        var child = new Account(
            AccountId.With("3100"),
            organisationId,
            "3100",
            "Service Revenue",
            AccountType.Revenue,
            parentId: parentId);
        var parent = new Account(
            parentId,
            organisationId,
            "3000",
            "Equity",
            AccountType.Equity,
            subAccounts: [child]);

        // Assert
        parent.SubAccounts.Count.ShouldBe(1);
        parent.SubAccounts[0].ShouldBe(child);
        child.ParentId.ShouldBe(parent.Id);
    }

    [Fact]
    public void ShouldExposeSubAccountsAsReadOnlyCollection()
    {
        // Arrange
        var organisation = new Organisation("Unity Club", "Accounting test organization");
        var organisationId = OrganisationId.With(organisation.Id);
        var child = new Account(AccountId.With("4100"), organisationId, "4100", "Donations", AccountType.Revenue);
        var parent = new Account(
            AccountId.With("4000"),
            organisationId,
            "4000",
            "Revenue",
            AccountType.Revenue,
            subAccounts: [child]);

        // Assert
        parent.SubAccounts.ShouldBeAssignableTo<IReadOnlyList<Account>>();
        parent.SubAccounts.Count.ShouldBe(1);
        parent.SubAccounts[0].ShouldBe(child);
    }
}