using System;

namespace Domain.Finances.Values;

public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Revenue = 4,
    Expense = 5
}

public static class AccountTypeExtensions
{
    public static (int Min, int Max) GetCodeRange(this AccountType type, string? name = null)
    {
        return type switch
        {
            AccountType.Asset => (1000, 1999),
            AccountType.Liability => (2000, 2999),
            AccountType.Equity => (3000, 3999),
            AccountType.Revenue => (4000, 4999),
            AccountType.Expense => name?.Contains("Admin", StringComparison.OrdinalIgnoreCase) == true || 
                                   name?.Contains("Overhead", StringComparison.OrdinalIgnoreCase) == true
                ? (6000, 6999)
                : (5000, 5999),
            _ => (0, 0)
        };
    }

    public static bool IsInRange(this AccountType type, string code, string? name = null)
    {
        if (!int.TryParse(code, out var value)) return false;
        var (min, max) = type.GetCodeRange(name);
        return value >= min && value <= max;
    }
}