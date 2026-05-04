using System.Collections.Generic;
using Jamaa.Application.Finances.Aggregates;
using Shouldly;
using Xunit;

namespace UnitTests.Finances;

public class AccountAggregateCascadeTests
{
    [Fact]
    public void BuildCascadeAccountIds_ShouldIncludeTargetAndAllActiveDescendants_WithoutSiblings_WhenDeactivating()
    {
        var accounts = CreateAccountsForDeactivation();

        var affectedIds = AccountStateCascadePlanner.BuildCascadeAccountIds(accounts, "root", isActiveTarget: false);

        affectedIds.ShouldBe(["root", "child-a", "grandchild-a1"]);
    }

    [Fact]
    public void BuildCascadeAccountIds_ShouldIncludeTargetAndAllInactiveDescendants_WithoutSiblings_WhenReactivating()
    {
        var accounts = CreateAccountsForReactivation();

        var affectedIds = AccountStateCascadePlanner.BuildCascadeAccountIds(accounts, "root", isActiveTarget: true);

        affectedIds.ShouldBe(["root", "child-a", "grandchild-a1"]);
    }

    private static IReadOnlyList<AccountStateSnapshot> CreateAccountsForDeactivation()
    {
        return
        [
            new AccountStateSnapshot("root", "1000", null, true),
            new AccountStateSnapshot("child-a", "1100", "root", true),
            new AccountStateSnapshot("child-b", "1200", "root", false),
            new AccountStateSnapshot("grandchild-a1", "1110", "child-a", true),
            new AccountStateSnapshot("sibling-root", "2000", null, true)
        ];
    }

    private static IReadOnlyList<AccountStateSnapshot> CreateAccountsForReactivation()
    {
        return
        [
            new AccountStateSnapshot("root", "1000", null, false),
            new AccountStateSnapshot("child-a", "1100", "root", false),
            new AccountStateSnapshot("child-b", "1200", "root", true),
            new AccountStateSnapshot("grandchild-a1", "1110", "child-a", false),
            new AccountStateSnapshot("sibling-root", "2000", null, true)
        ];
    }
}



