using Domain.Accounting.Values;
using Domain.Organisation.Values;
using Jamaa.Application.Shared;

namespace Jamaa.Application.Accounting.Events;

public record AccountCreated(
    OrganisationId OrganisationId,
    AccountId AccountId,
    string Code,
    string Name,
    AccountType Type,
    AccountId? ParentId,
    string Description = "") : IJamaaEvent
{
    public string EntityId => AccountId.Value;
}

public record AccountUpdated(
    OrganisationId OrganisationId,
    AccountId AccountId,
    string Code,
    string Name,
    AccountType Type,
    AccountId? ParentId,
    string Description = "") : IJamaaEvent
{
    public string EntityId => AccountId.Value;
}

public record AccountDeleted(
    OrganisationId OrganisationId,
    AccountId AccountId) : IJamaaEvent
{
    public string EntityId => AccountId.Value;
}

public record AccountDeactivated(
    OrganisationId OrganisationId,
    AccountId AccountId) : IJamaaEvent
{
    public string EntityId => AccountId.Value;
}

public record AccountReactivated(
    OrganisationId OrganisationId,
    AccountId AccountId) : IJamaaEvent
{
    public string EntityId => AccountId.Value;
}

public record AccountOpeningBalanceSet(
    OrganisationId OrganisationId,
    AccountId AccountId,
    FiscalYearId FiscalYearId,
    AccountingPeriodId AccountingPeriodId,
    decimal OpeningBalance) : IJamaaEvent
{
    public string EntityId => AccountId.Value;
}