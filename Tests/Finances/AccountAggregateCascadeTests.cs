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

    [Fact]
    public void BuildCascadeAccountIds_ShouldDeactivateParent_WhenAllDirectChildrenBecomeInactive()
    {
        var accounts = new List<AccountStateSnapshot>
        {
            new("parent", "1000", null, true),
            new("child-a", "1100", "parent", true),
            new("child-b", "1200", "parent", false)
        };

        var affectedIds = AccountStateCascadePlanner.BuildCascadeAccountIds(accounts, "child-a", isActiveTarget: false);

        affectedIds.ShouldBe(["child-a", "parent"]);
    }

    [Fact]
    public void BuildCascadeAccountIds_ShouldReactivateParent_WhenAllDirectChildrenBecomeActive()
    {
        var accounts = new List<AccountStateSnapshot>
        {
            new("parent", "1000", null, false),
            new("child-a", "1100", "parent", false),
            new("child-b", "1200", "parent", true)
        };

        var affectedIds = AccountStateCascadePlanner.BuildCascadeAccountIds(accounts, "child-a", isActiveTarget: true);

        affectedIds.ShouldBe(["child-a", "parent"]);
    }

    [Fact]
    public void BuildCascadeAccountIds_ShouldNotChangeParent_WhenDirectChildrenRemainMixed()
    {
        var accounts = new List<AccountStateSnapshot>
        {
            new("parent", "1000", null, true),
            new("child-a", "1100", "parent", true),
            new("child-b", "1200", "parent", true)
        };

        var affectedIds = AccountStateCascadePlanner.BuildCascadeAccountIds(accounts, "child-a", isActiveTarget: false);

        affectedIds.ShouldBe(["child-a"]);
    }

    [Fact]
    public void BuildCascadeAccountIds_ShouldBubbleStateToAncestors_WhenEachLevelBecomesUniform()
    {
        var accounts = new List<AccountStateSnapshot>
        {
            new("grandparent", "0900", null, true),
            new("parent", "1000", "grandparent", true),
            new("uncle", "2000", "grandparent", false),
            new("child-a", "1100", "parent", true),
            new("child-b", "1200", "parent", false)
        };

        var affectedIds = AccountStateCascadePlanner.BuildCascadeAccountIds(accounts, "child-a", isActiveTarget: false);

        affectedIds.ShouldBe(["child-a", "parent", "grandparent"]);
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




