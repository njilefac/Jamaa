using System.Threading.Tasks;
using Domain.Finances.Queries;
using Jamaa.Data.Models.Finances;

namespace Jamaa.Data.Queries.Finances;

public interface IAccountingSettingsQueryHandler
{
    Task<AccountingSettingsData?> Get(GetAccountingSettingsByOrganisation query);
}

