using Domain.Accounting.Entities;
using Domain.Accounting.Queries;
using Domain.Members;
using Domain.Members.Queries;
using Domain.Organisation;
using Domain.Organisation.Queries;
using Jamaa.Data.Queries.Finances;
using Jamaa.Data.Queries.Members;
using Jamaa.Data.Repositories.Organisations;

namespace Jamaa.Application.Shared;

public class QueryProcessor(
    IOrganisationQueryHandler organisationQueryHandler,
    IMembersQueryHandler membersQueryHandler,
    IAccountQueryHandler accountQueryHandler,
    IFiscalCalendarQueryHandler fiscalCalendarQueryHandler,
    IAccountingSettingsQueryHandler accountingSettingsQueryHandler)
    : IQueryProcessor
{
    // Integration: fetch and map organisation summaries from the data layer into domain query types.
    public async Task<List<OrganisationSummary>> Get(GetAllOrganisations query)
    {
        var organisations = await organisationQueryHandler.HandleQuery(query);
        return organisations.Select(organisation => organisation.ToDomainModel()).ToList();
    }

    // Integration: fetch and map one organisation summary by name into a domain query type.
    public async Task<OrganisationSummary?> Get(GetOrganisationByName query)
    {
        var organisation = await organisationQueryHandler.HandleQuery(query);
        return organisation?.ToDomainModel();
    }

    // Integration: fetch and map member profiles from the data layer into domain query types.
    public async Task<IList<MemberProfile>> Get(GetMembersByOrganisation query)
    {
        var members = await membersQueryHandler.Get(query);
        return members.Select(member => member.ToDomainModel()).ToList();
    }

    // Integration: fetch and map accounts from the data layer into domain entities.
    public async Task<IList<Account>> Get(GetAccountsByOrganisation query)
    {
        var accounts = await accountQueryHandler.Get(query);
        return accounts.Select(account => account.ToDomainModel()).ToList();
    }

    // Integration: fetch and map fiscal years from the data layer into domain entities.
    public async Task<IList<FiscalYear>> Get(GetFiscalYearsByOrganisation query)
    {
        var fiscalYears = await fiscalCalendarQueryHandler.Get(query);
        return fiscalYears.Select(fiscalYear => fiscalYear.ToDomainModel()).ToList();
    }

    // Integration: fetch and map accounting settings from the data layer into a domain entity.
    public async Task<AccountingSettings?> Get(GetAccountingSettingsByOrganisation query)
    {
        var settings = await accountingSettingsQueryHandler.Get(query);
        return settings?.ToDomainModel();
    }
}