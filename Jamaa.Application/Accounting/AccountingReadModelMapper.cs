using Domain.Accounting.Entities;
using Domain.Accounting.Values;
using Jamaa.Application.Accounting.Models;
using DataModels = Jamaa.Data.Models.Finances;

namespace Jamaa.Application.Accounting;

internal static class AccountingReadModelMapper
{
    internal static AccountData ToPresentationModel(this DataModels.AccountData account)
    {
        return new AccountData
        {
            Id = account.Id,
            OrganisationId = account.OrganisationId,
            Code = account.Code,
            Name = account.Name,
            Description = account.Description,
            Type = account.Type,
            ParentId = account.ParentId,
            IsActive = account.IsActive
        };
    }

    internal static ChartOfAccountsData ToChartOfAccountsReadModel(
        this IList<Account> accounts,
        string organisationId)
    {
        return new ChartOfAccountsData
        {
            OrganisationId = organisationId,
            Accounts = accounts.Select(account => account.ToPresentationModel()).ToList()
        };
    }

    private static AccountData ToPresentationModel(this Account account)
    {
        return new AccountData
        {
            Id = account.Id.Value,
            OrganisationId = account.OrganisationId.Value,
            Code = account.Code,
            Name = account.Name,
            Description = account.Description,
            Type = account.Type,
            ParentId = account.ParentId?.Value,
            IsActive = account.IsActive
        };
    }

    internal static AccountingPeriodData ToPresentationModel(this DataModels.AccountingPeriodData period)
    {
        return new AccountingPeriodData
        {
            Id = period.Id,
            FiscalYearId = period.FiscalYearId,
            OrganisationId = period.OrganisationId,
            SequenceNumber = period.SequenceNumber,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            IsLocked = period.IsLocked
        };
    }

    internal static AccountingPeriodData ToPresentationModel(this AccountingPeriod period)
    {
        return new AccountingPeriodData
        {
            Id = period.Id.Value,
            FiscalYearId = period.FiscalYearId.Value,
            OrganisationId = period.OrganisationId.Value,
            SequenceNumber = period.SequenceNumber,
            StartDate = period.Begin,
            EndDate = period.End,
            IsLocked = period.IsLocked
        };
    }

    internal static FiscalYearData ToPresentationModel(this FiscalYear fiscalYear)
    {
        return new FiscalYearData
        {
            Id = fiscalYear.Id.Value,
            OrganisationId = fiscalYear.OrganisationId.Value,
            StartDate = fiscalYear.Begin,
            EndDate = fiscalYear.End,
            IsLocked = fiscalYear.IsLocked,
            Periods = fiscalYear.Periods
                .OrderBy(period => period.SequenceNumber)
                .ThenBy(period => period.Begin)
                .Select(period => period.ToPresentationModel())
                .ToList()
        };
    }

    internal static AccountingAvailableCurrencyData ToPresentationModel(
        this DataModels.AccountingAvailableCurrencyData currency, string? organisationId = null)
    {
        return new AccountingAvailableCurrencyData
        {
            OrganisationId = organisationId ?? currency.OrganisationId,
            CurrencyCode = currency.CurrencyCode,
            CurrencySymbol = currency.CurrencySymbol
        };
    }

    private static AccountingAvailableCurrencyData ToPresentationModel(this Currency currency, string organisationId)
    {
        return new AccountingAvailableCurrencyData
        {
            OrganisationId = organisationId,
            CurrencyCode = currency.Code,
            CurrencySymbol = currency.Symbol
        };
    }

    internal static AccountingSettingsData ToPresentationModel(this AccountingSettings settings)
    {
        return new AccountingSettingsData
        {
            OrganisationId = settings.OrganisationId.Value,
            BaseCurrency = settings.BaseCurrency,
            DateFormat = settings.DateFormat,
            DecimalPrecision = settings.DecimalPrecision,
            AvailableCurrencies = settings.AvailableCurrencies
                .Select(currency => currency.ToPresentationModel(settings.OrganisationId.Value))
                .ToList()
        };
    }

    internal static AccountingPeriodBalanceData ToPresentationModel(this DataModels.AccountingPeriodBalanceData balance)
    {
        return new AccountingPeriodBalanceData
        {
            Id = balance.Id,
            AccountId = balance.AccountId,
            FiscalYearId = balance.FiscalYearId,
            AccountingPeriodId = balance.AccountingPeriodId,
            OrganisationId = balance.OrganisationId,
            OpeningBalance = balance.OpeningBalance,
            ClosingBalance = balance.ClosingBalance
        };
    }

}