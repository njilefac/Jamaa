using System;
using System.Collections.Generic;
using System.Linq;

using Domain.Accounting.Values;

namespace Domain.Accounting.Service;

public static class AccountCodeSuggester
{
    public readonly record struct AccountCodeContext(string Id, string Code, AccountType Type, string? ParentAccountId);

    public static (int Min, int Max) GetCodeRange(AccountType type, string accountName)
    {
        if (type == AccountType.Expense && accountName.Contains("fee", StringComparison.OrdinalIgnoreCase))
            return (6000, 6999);

        return type switch
        {
            AccountType.Asset => (1000, 1999),
            AccountType.Liability => (2000, 2999),
            AccountType.Equity => (3000, 3999),
            AccountType.Revenue => (4000, 4999),
            AccountType.Expense => (5000, 5999),
            _ => (1000, 9999)
        };
    }

    public static bool IsCodeInRange(AccountType type, string code, string accountName)
    {
        if (!int.TryParse(code, out var numericCode)) return false;

        var (min, max) = GetCodeRange(type, accountName);
        return numericCode >= min && numericCode <= max;
    }

    public static int SuggestNextCode(
        AccountType type,
        string accountName,
        string? parentAccountId,
        IEnumerable<AccountCodeContext> existingAccounts)
    {
        var (min, max) = GetCodeRange(type, accountName);
        var accounts = existingAccounts.ToList();
        var existingCodesInLevel = accounts
            .Where(account =>
                account.Type == type &&
                string.Equals(account.ParentAccountId, parentAccountId, StringComparison.Ordinal))
            .Select(account => int.TryParse(account.Code, out var parsed) ? parsed : (int?)null)
            .Where(parsed => parsed is >= 0)
            .Select(parsed => parsed!.Value)
            .Where(code => code >= min && code <= max)
            .Distinct()
            .OrderByDescending(code => code)
            .ToList();

        if (existingCodesInLevel.Count == 0)
            return parentAccountId is null
                ? GetFirstTopLevelCode(min, max, type, accountName)
                : SuggestFirstChildCode(type, accountName, parentAccountId, accounts, min, max);

        var usedCodes = existingCodesInLevel.ToHashSet();
        var highestUsedCode = existingCodesInLevel[0];
        foreach (var step in parentAccountId is null ? new[] { 100, 50, 25 } : new[] { 10, 5, 4, 3, 2, 1 })
        {
            var candidate = highestUsedCode + step;
            if (candidate > max) continue;
            if (candidate < min) continue;
            if (!usedCodes.Contains(candidate)) return candidate;
        }

        for (var candidate = min; candidate <= max; candidate++)
            if (!usedCodes.Contains(candidate))
                return candidate;

        throw BuildAccountNumberBlockExhaustedException(type, accountName, parentAccountId, min, max);
    }

    private static int SuggestFirstChildCode(
        AccountType type,
        string accountName,
        string parentAccountId,
        IReadOnlyCollection<AccountCodeContext> accounts,
        int min,
        int max)
    {
        var parentCode = ResolveParentCode(type, accountName, parentAccountId, accounts);
        foreach (var step in new[] { 10, 5, 4, 3, 2, 1 })
        {
            var candidate = parentCode + step;
            if (candidate > max) continue;
            if (candidate < min) continue;
            return candidate;
        }

        throw BuildAccountNumberBlockExhaustedException(type, accountName, parentAccountId, min, max);
    }

    private static int ResolveParentCode(
        AccountType type,
        string accountName,
        string parentAccountId,
        IReadOnlyCollection<AccountCodeContext> accounts)
    {
        var parent = accounts.FirstOrDefault(account => string.Equals(account.Id, parentAccountId, StringComparison.Ordinal));
        if (string.IsNullOrWhiteSpace(parent.Id) || !int.TryParse(parent.Code, out var parentCode))
            throw new InvalidOperationException(
                $"Unable to suggest an account code for account type '{type}' and account name '{accountName}': parent account '{parentAccountId}' was not found or has a non-numeric code.");

        return parentCode;
    }

    private static int GetFirstTopLevelCode(int min, int max, AccountType type, string accountName)
    {
        var firstTopLevelCode = min + 100;
        if (firstTopLevelCode <= max) return firstTopLevelCode;

        throw BuildAccountNumberBlockExhaustedException(type, accountName, null, min, max);
    }

    private static InvalidOperationException BuildAccountNumberBlockExhaustedException(
        AccountType type,
        string accountName,
        string? parentAccountId,
        int min,
        int max)
    {
        var capacity = max - min + 1;
        var label = string.IsNullOrWhiteSpace(accountName) ? "(no account name)" : accountName.Trim();
        var hierarchy = parentAccountId is null
            ? "top-level accounts"
            : $"children of parent '{parentAccountId}'";
        return new InvalidOperationException(
            $"No available account numbers remain in block [{min}-{max}] for account type '{type}', account name '{label}', and hierarchy '{hierarchy}'. " +
            $"All {capacity} numbers are already used in this block.");
    }
}
