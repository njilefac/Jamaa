using Domain.Accounting.Values;
using Domain.Organisation.Values;

namespace Jamaa.Application.Accounting.Commands;

public record CreateAccount(
    OrganisationId OrganisationId,
    AccountId AccountId,
    string Code,
    string Name,
    AccountType Type,
    AccountId? ParentId,
    string Description = "");

public record UpdateAccount(
    OrganisationId OrganisationId,
    AccountId AccountId,
    string Code,
    string Name,
    AccountType Type,
    AccountId? ParentId,
    string Description = "");

public record DeleteAccount(
    OrganisationId OrganisationId,
    AccountId AccountId);

public record DeactivateAccount(
    OrganisationId OrganisationId,
    AccountId AccountId);

public record ReactivateAccount(
    OrganisationId OrganisationId,
    AccountId AccountId);