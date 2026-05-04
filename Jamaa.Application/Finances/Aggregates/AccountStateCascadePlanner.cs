namespace Jamaa.Application.Finances.Aggregates;

public static class AccountStateCascadePlanner
{
    // Operation: returns one account id plus active-state-changing descendants in deterministic breadth-first order.
    public static IReadOnlyList<string> BuildCascadeAccountIds(
        IReadOnlyList<AccountStateSnapshot> accounts,
        string rootAccountId,
        bool isActiveTarget)
    {
        var accountsById = accounts.ToDictionary(account => account.Id, StringComparer.Ordinal);

        return GetAccountAndDescendantIds(accountsById, rootAccountId)
            .Where(accountId => accountsById.TryGetValue(accountId, out var account) && account.IsActive != isActiveTarget)
            .ToList();
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
            {
                if (visited.Add(childId))
                {
                    queue.Enqueue(childId);
                }
            }
        }

        return orderedIds;
    }
}

public sealed record AccountStateSnapshot(string Id, string Code, string? ParentId, bool IsActive);

