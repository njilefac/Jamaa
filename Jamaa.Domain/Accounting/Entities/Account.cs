using System.Collections.Generic;
using System.Linq;
using Domain.Accounting.Values;
using Domain.Organisation.Values;

namespace Domain.Accounting.Entities;

public sealed record Account
{
    private readonly List<Account> _subAccounts;

    public Account(
        AccountId id,
        OrganisationId organisationId,
        string code,
        string name,
        AccountType type,
        string description = "",
        AccountId? parentId = null,
        bool isActive = true,
        IEnumerable<Account>? subAccounts = null)
    {
        Id = id;
        OrganisationId = organisationId;
        Code = code;
        Name = name;
        Type = type;
        Description = description;
        ParentId = parentId;
        IsActive = isActive;
        _subAccounts = subAccounts?.ToList() ?? [];
    }

    public AccountId Id { get; }
    public OrganisationId OrganisationId { get; }
    public string Code { get; }
    public string Name { get; }
    public string Description { get; }
    public AccountType Type { get; }
    public AccountId? ParentId { get; }
    public bool IsActive { get; }
    public IReadOnlyList<Account> SubAccounts => _subAccounts.AsReadOnly();
}