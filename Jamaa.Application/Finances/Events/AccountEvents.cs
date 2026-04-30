using Domain.Finances.Values;
using Domain.Organisation.Values;
using Jamaa.Application.Shared;

namespace Jamaa.Application.Finances.Events;

public record AccountCreated(
    OrganisationId OrganisationId,
    AccountId AccountId,
    string Code,
    string Name,
    AccountType Type,
    AccountId? ParentId) : ILibotaEvent
{
    public string EntityId => AccountId.Value;
}

public record AccountUpdated(
    OrganisationId OrganisationId,
    AccountId AccountId,
    string Code,
    string Name,
    AccountType Type,
    AccountId? ParentId) : ILibotaEvent
{
    public string EntityId => AccountId.Value;
}

public record AccountDeleted(
    OrganisationId OrganisationId,
    AccountId AccountId) : ILibotaEvent
{
    public string EntityId => AccountId.Value;
}
