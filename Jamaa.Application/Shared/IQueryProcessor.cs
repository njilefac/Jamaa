using Domain.Accounting.Entities;
using Domain.Accounting.Queries;
using Domain.Members;
using Domain.Members.Queries;
using Domain.Organisation;
using Domain.Organisation.Queries;

namespace Jamaa.Application.Shared;

public interface IQueryProcessor
{
    Task<List<OrganisationSummary>> Get(GetAllOrganisations query);
    Task<OrganisationSummary?> Get(GetOrganisationByName query);

    Task<IList<MemberProfile>> Get(GetMembersByOrganisation query);
    Task<IList<Account>> Get(GetAccountsByOrganisation query);
    Task<IList<FiscalYear>> Get(GetFiscalYearsByOrganisation query);
    Task<AccountingSettings?> Get(GetAccountingSettingsByOrganisation query);
}