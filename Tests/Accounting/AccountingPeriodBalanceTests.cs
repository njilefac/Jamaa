using Domain.Accounting.Entities;
using Domain.Accounting.Values;
using Domain.Organisation.Values;
using Xunit;

namespace Tests.Accounting;

public class AccountingPeriodBalanceTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var id = AccountingPeriodBalanceId.New();
        var accountId = AccountId.With("acc-1");
        var fiscalYearId = FiscalYearId.With("fy-1");
        var periodId = AccountingPeriodId.With("p-1");
        var orgId = OrganisationId.With("org-1");
        var currency = new Currency("USD", "$");
        var opening = new MoneyAmount(100m, currency);
        var closing = new MoneyAmount(150m, currency);

        // Act
        var balance = new AccountingPeriodBalance(
            id,
            accountId,
            fiscalYearId,
            periodId,
            orgId,
            opening,
            closing
        );

        // Assert
        Assert.Equal(id, balance.Id);
        Assert.Equal(accountId, balance.AccountId);
        Assert.Equal(fiscalYearId, balance.FiscalYearId);
        Assert.Equal(periodId, balance.AccountingPeriodId);
        Assert.Equal(orgId, balance.OrganisationId);
        Assert.Equal(opening, balance.OpeningBalance);
        Assert.Equal(closing, balance.ClosingBalance);
    }
}
