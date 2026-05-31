using System;
using System.Linq;
using Domain.Accounting.Service;
using Domain.Accounting.Values;
using Shouldly;
using Xunit;

namespace UnitTests.Accounting;

public class AccountCodeSuggesterTests
{
    [Fact]
    public void SuggestNextCode_ShouldStartFirstTopLevelAtHundredGap()
    {
        var next = AccountCodeSuggester.SuggestNextCode(
            AccountType.Asset,
            "Cash",
            null,
            []);

        next.ShouldBe(1100);
    }

    [Fact]
    public void SuggestNextCode_ShouldPreferTopLevelGapOfHundredThenFiftyThenTwentyFive()
    {
        var next = AccountCodeSuggester.SuggestNextCode(
            AccountType.Asset,
            "Cash",
            null,
            [
                new AccountCodeSuggester.AccountCodeContext("a-1100", "1100", AccountType.Asset, null),
                new AccountCodeSuggester.AccountCodeContext("a-1200", "1200", AccountType.Asset, null),
                new AccountCodeSuggester.AccountCodeContext("a-1300", "1300", AccountType.Asset, null),
                new AccountCodeSuggester.AccountCodeContext("a-1400", "1400", AccountType.Asset, null),
                new AccountCodeSuggester.AccountCodeContext("a-1500", "1500", AccountType.Asset, null),
                new AccountCodeSuggester.AccountCodeContext("a-1600", "1600", AccountType.Asset, null),
                new AccountCodeSuggester.AccountCodeContext("a-1700", "1700", AccountType.Asset, null),
                new AccountCodeSuggester.AccountCodeContext("a-1800", "1800", AccountType.Asset, null),
                new AccountCodeSuggester.AccountCodeContext("a-1900", "1900", AccountType.Asset, null),
                new AccountCodeSuggester.AccountCodeContext("a-1950", "1950", AccountType.Asset, null)
            ]);

        next.ShouldBe(1975);
    }

    [Fact]
    public void SuggestNextCode_ShouldStartFirstChildAtParentCodePlusTen()
    {
        var next = AccountCodeSuggester.SuggestNextCode(
            AccountType.Asset,
            "Cash",
            "parent-1",
            [new AccountCodeSuggester.AccountCodeContext("parent-1", "1100", AccountType.Asset, null)]);

        next.ShouldBe(1110);
    }

    [Fact]
    public void SuggestNextCode_ShouldPreferSiblingGapOfTen()
    {
        var next = AccountCodeSuggester.SuggestNextCode(
            AccountType.Asset,
            "Cash",
            "parent-1",
            [
                new AccountCodeSuggester.AccountCodeContext("parent-1", "1100", AccountType.Asset, null),
                new AccountCodeSuggester.AccountCodeContext("child-1", "1110", AccountType.Asset, "parent-1"),
                new AccountCodeSuggester.AccountCodeContext("child-2", "1115", AccountType.Asset, "parent-1")
            ]);

        next.ShouldBe(1125);
    }

    [Fact]
    public void SuggestNextCode_ShouldFallbackToSiblingGapOfFive()
    {
        var next = AccountCodeSuggester.SuggestNextCode(
            AccountType.Asset,
            "Cash",
            "parent-1",
            [
                new AccountCodeSuggester.AccountCodeContext("parent-1", "1980", AccountType.Asset, null),
                new AccountCodeSuggester.AccountCodeContext("child-1", "1990", AccountType.Asset, "parent-1")
            ]);

        next.ShouldBe(1995);
    }

    [Fact]
    public void SuggestNextCode_ShouldUseSelectedParentSiblingsOnly()
    {
        var next = AccountCodeSuggester.SuggestNextCode(
            AccountType.Asset,
            "Cash",
            "parent-1",
            [
                new AccountCodeSuggester.AccountCodeContext("parent-1", "1300", AccountType.Asset, null),
                new AccountCodeSuggester.AccountCodeContext("parent-2", "1400", AccountType.Asset, null),
                new AccountCodeSuggester.AccountCodeContext("other-child", "1410", AccountType.Asset, "parent-2"),
                new AccountCodeSuggester.AccountCodeContext("liability", "1310", AccountType.Liability, "parent-1")
            ]);

        next.ShouldBe(1310);
    }

    [Fact]
    public void SuggestNextCode_ShouldThrow_WhenLevelRangeIsExhausted()
    {
        var existingCodes =
            Enumerable.Range(1000, 1000)
                .Select(code =>
                    new AccountCodeSuggester.AccountCodeContext($"child-{code}", code.ToString(), AccountType.Asset,
                        "parent-1"))
                .Prepend(new AccountCodeSuggester.AccountCodeContext("parent-1", "1000", AccountType.Asset, null));

        var exception = Should.Throw<InvalidOperationException>(() =>
            AccountCodeSuggester.SuggestNextCode(AccountType.Asset, "Cash", "parent-1", existingCodes));

        exception.Message.ShouldContain("[1000-1999]");
        exception.Message.ShouldContain("children of parent 'parent-1'");
        exception.Message.ShouldContain("All 1000 numbers are already used");
    }
}
