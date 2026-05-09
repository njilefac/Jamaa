namespace Jamaa.Application.Accounting.Aggregates;

public static class AccountStateCascadePlanner
{
    // Operation: returns state-changing descendants, then bubbles to ancestors when all direct children share target state.
    public static IReadOnlyList<string> BuildCascadeAccountIds(
        IReadOnlyList<AccountStateSnapshot> accounts,
        string rootAccountId,
        bool isActiveTarget)
    {
        var accountsById = accounts.ToDictionary(account => account.Id, StringComparer.Ordinal);
        var plannedIds = GetAccountAndDescendantIds(accountsById, rootAccountId)
            .Where(accountId =>
                accountsById.TryGetValue(accountId, out var account) && account.IsActive != isActiveTarget)
            .ToList();

        var simulatedState = accountsById.ToDictionary(account => account.Key, account => account.Value.IsActive,
            StringComparer.Ordinal);
        foreach (var plannedId in plannedIds) simulatedState[plannedId] = isActiveTarget;

        foreach (var ancestorId in GetAncestorIds(accountsById, rootAccountId))
        {
            var childIds = GetDirectChildIds(accountsById, ancestorId);
            if (childIds.Count == 0) continue;

            var allChildrenMatchTarget = childIds
                .All(childId => simulatedState.TryGetValue(childId, out var childIsActive) &&
                                childIsActive == isActiveTarget);
            if (!allChildrenMatchTarget) continue;

            if (!simulatedState.TryGetValue(ancestorId, out var ancestorIsActive) ||
                ancestorIsActive == isActiveTarget) continue;

            plannedIds.Add(ancestorId);
            simulatedState[ancestorId] = isActiveTarget;
        }

        return plannedIds;
    }

    // Operation: returns one account id plus all descendant ids using deterministic breadth-first traversal.
    private static IReadOnlyList<string> GetAccountAndDescendantIds(
        IReadOnlyDictionary<string, AccountStateSnapshot> accountsById,
        string rootAccountId)
    {
        var orderedIds = new List<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>();

        queue.Enqueue(rootAccountId);
        visited.Add(rootAccountId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            orderedIds.Add(currentId);

            var childIds = accountsById.Values
                .Where(account => account.ParentId == currentId)
                .OrderBy(account => account.Code, StringComparer.Ordinal)
                .ThenBy(account => account.Id, StringComparer.Ordinal)
                .Select(account => account.Id);

            foreach (var childId in childIds)
                if (visited.Add(childId))
                    queue.Enqueue(childId);
        }

        return orderedIds;
    }

    // Operation: returns direct child ids in deterministic order.
    private static IReadOnlyList<string> GetDirectChildIds(
        IReadOnlyDictionary<string, AccountStateSnapshot> accountsById,
        string parentAccountId)
    {
        return accountsById.Values
            .Where(account => account.ParentId == parentAccountId)
            .OrderBy(account => account.Code, StringComparer.Ordinal)
            .ThenBy(account => account.Id, StringComparer.Ordinal)
            .Select(account => account.Id)
            .ToList();
    }

    // Operation: returns ancestor ids from nearest parent up to the root.
    private static IEnumerable<string> GetAncestorIds(
        IReadOnlyDictionary<string, AccountStateSnapshot> accountsById,
        string accountId)
    {
        if (!accountsById.TryGetValue(accountId, out var account)) yield break;

        var parentId = account.ParentId;
        while (!string.IsNullOrWhiteSpace(parentId) && accountsById.TryGetValue(parentId, out var parent))
        {
            yield return parent.Id;
            parentId = parent.ParentId;
        }
    }
}

public sealed record AccountStateSnapshot(string Id, string Code, string? ParentId, bool IsActive);