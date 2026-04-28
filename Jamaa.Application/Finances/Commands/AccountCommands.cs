using Domain.Finances.Values;
using Domain.Organisation.Values;

namespace Jamaa.Application.Finances.Commands;

public record CreateAccount(
    OrganisationId OrganisationId,
    AccountId AccountId,
    string Code,
    string Name,
    AccountType Type,
    AccountId? ParentId);

public record UpdateAccount(
    OrganisationId OrganisationId,
    AccountId AccountId,
    string Code,
    string Name,
    AccountType Type,
    AccountId? ParentId);

public record DeleteAccount(
    OrganisationId OrganisationId,
    AccountId AccountId);
