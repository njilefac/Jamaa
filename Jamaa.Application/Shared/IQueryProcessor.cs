using Domain.Members.Queries;
using Domain.Finances.Queries;
using Domain.Organisation.Queries;
using Jamaa.Data.Models.Finances;
using Jamaa.Data.Models.Members;
using Jamaa.Data.Models.Organisation;

namespace Jamaa.Application.Shared;

public interface IQueryProcessor
{
    Task<List<OrganisationData>> Get(GetAllOrganisations query);
    Task<OrganisationData?> Get(GetOrganisationByName query);

    Task<IList<MemberData>> Get(GetMembersByOrganisation query);
    Task<IList<FiscalYearData>> Get(GetFiscalYearsByOrganisation query);
    Task<AccountingSettingsData?> Get(GetAccountingSettingsByOrganisation query);
}