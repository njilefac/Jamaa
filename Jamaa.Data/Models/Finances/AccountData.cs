using System.ComponentModel.DataAnnotations;
using Domain.Finances.Values;

namespace Jamaa.Data.Models.Finances;

public class AccountData
{
    [Key] public required string Id { get; set; }
    public required string OrganisationId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public string? ParentId { get; set; }
    
    public AccountData? Parent { get; set; }
}
