using Domain.Accounting.Entities;
using Domain.Accounting.Values;
using Domain.Members;
using Domain.Organisation;
using Domain.Organisation.Values;
using Jamaa.Data.Models.Finances;
using Jamaa.Data.Models.Members;
using Jamaa.Data.Models.Organisation;

namespace Jamaa.Application.Shared;

internal static class QueryProcessorDomainMapper
{
    internal static OrganisationSummary ToDomainModel(this OrganisationData organisation)
    {
        return new OrganisationSummary(organisation.Id, organisation.Name, organisation.Description);
    }

    internal static MemberProfile ToDomainModel(this MemberData member)
    {
        return new MemberProfile(
            member.Id,
            member.Registration.Id,
            OrganisationId.With(member.OrganisationId),
            member.FirstName,
            member.MiddleName,
            member.LastName,
            member.Gender,
            member.Registration.MembershipType,
            member.Registration.Status,
            member.Registration.StartDate,
            member.Registration.EndDate,
            member.PictureData);
    }

    internal static Account ToDomainModel(this AccountData account)
    {
        return new Account(
            AccountId.With(account.Id),
            OrganisationId.With(account.OrganisationId),
            account.Code,
            account.Name,
            account.Type,
            account.Description,
            string.IsNullOrWhiteSpace(account.ParentId) ? null : AccountId.With(account.ParentId),
            account.IsActive,
            account.IsContraAccount);
    }

    internal static AccountingPeriod ToDomainModel(this AccountingPeriodData period)
    {
        return new AccountingPeriod(
            AccountingPeriodId.With(period.Id),
            FiscalYearId.With(period.FiscalYearId),
            OrganisationId.With(period.OrganisationId),
            period.SequenceNumber,
            period.StartDate,
            period.EndDate,
            period.IsLocked);
    }

    internal static FiscalYear ToDomainModel(this FiscalYearData fiscalYear)
    {
        var domainFiscalYear = new FiscalYear(
            FiscalYearId.With(fiscalYear.Id),
            OrganisationId.With(fiscalYear.OrganisationId),
            fiscalYear.StartDate,
            fiscalYear.EndDate,
            fiscalYear.IsLocked);

        foreach (var period in fiscalYear.Periods
                     .OrderBy(current => current.SequenceNumber)
                     .ThenBy(current => current.StartDate))
            domainFiscalYear.AddPeriod(period.ToDomainModel());

        return domainFiscalYear;
    }

    internal static AccountingSettings ToDomainModel(this AccountingSettingsData settings)
    {
        return new AccountingSettings(
            OrganisationId.With(settings.OrganisationId),
            settings.BaseCurrency,
            settings.DateFormat,
            settings.DecimalPrecision,
            settings.ThousandSeparator,
            settings.AvailableCurrencies.Select(currency =>
                new Currency(currency.CurrencyCode, currency.CurrencySymbol)));
    }
}
