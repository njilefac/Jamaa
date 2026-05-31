using System.Linq;
using Domain.Accounting.Values;
using Jamaa.Application.Accounting.Aggregates;
using Shouldly;
using Xunit;

namespace UnitTests.Finances;

public class AccountingSettingsAggregateTests
{
    // --- TryValidateBaseCurrency ---

    [Fact]
    public void TryValidateBaseCurrency_ShouldReturnTrue_WhenCurrencyIsValid()
    {
        var result = AccountingSettingsAggregate.TryValidateBaseCurrency("KES", out var error);

        result.ShouldBeTrue();
        error.ShouldBeEmpty();
    }

    [Fact]
    public void TryValidateBaseCurrency_ShouldReturnFalse_WhenCurrencyIsEmpty()
    {
        var result = AccountingSettingsAggregate.TryValidateBaseCurrency(string.Empty, out var error);

        result.ShouldBeFalse();
        error.ShouldNotBeEmpty();
    }

    [Fact]
    public void TryValidateBaseCurrency_ShouldReturnFalse_WhenCurrencyIsWhiteSpace()
    {
        var result = AccountingSettingsAggregate.TryValidateBaseCurrency("   ", out var error);

        result.ShouldBeFalse();
        error.ShouldNotBeEmpty();
    }

    [Fact]
    public void TryValidateBaseCurrency_ShouldReturnFalse_WhenCurrencyExceedsMaxLength()
    {
        var result = AccountingSettingsAggregate.TryValidateBaseCurrency("TOOLONGCURRENCYCODE", out var error);

        result.ShouldBeFalse();
        error.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("KES")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    public void TryValidateBaseCurrency_ShouldReturnTrue_ForCommonIsoCodes(string currency)
    {
        var result = AccountingSettingsAggregate.TryValidateBaseCurrency(currency, out var error);

        result.ShouldBeTrue();
        error.ShouldBeEmpty();
    }

    // --- TryValidateDateFormat ---

    [Fact]
    public void TryValidateDateFormat_ShouldReturnTrue_WhenFormatIsNonEmpty()
    {
        var result = AccountingSettingsAggregate.TryValidateDateFormat("DD/MM/YYYY", out var error);

        result.ShouldBeTrue();
        error.ShouldBeEmpty();
    }

    [Fact]
    public void TryValidateDateFormat_ShouldReturnFalse_WhenFormatIsEmpty()
    {
        var result = AccountingSettingsAggregate.TryValidateDateFormat(string.Empty, out var error);

        result.ShouldBeFalse();
        error.ShouldNotBeEmpty();
    }

    [Fact]
    public void TryValidateDateFormat_ShouldReturnFalse_WhenFormatIsWhiteSpace()
    {
        var result = AccountingSettingsAggregate.TryValidateDateFormat("   ", out var error);

        result.ShouldBeFalse();
        error.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("DD/MM/YYYY")]
    [InlineData("MM/DD/YYYY")]
    [InlineData("YYYY-MM-DD")]
    public void TryValidateDateFormat_ShouldReturnTrue_ForKnownFormats(string format)
    {
        var result = AccountingSettingsAggregate.TryValidateDateFormat(format, out var error);

        result.ShouldBeTrue();
        error.ShouldBeEmpty();
    }

    // --- TryValidateDecimalPrecision ---

    [Fact]
    public void TryValidateDecimalPrecision_ShouldReturnTrue_ForDefault2()
    {
        var result = AccountingSettingsAggregate.TryValidateDecimalPrecision(2, out var error);

        result.ShouldBeTrue();
        error.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void TryValidateDecimalPrecision_ShouldReturnTrue_ForBoundaryValues(int precision)
    {
        var result = AccountingSettingsAggregate.TryValidateDecimalPrecision(precision, out var error);

        result.ShouldBeTrue();
        error.ShouldBeEmpty();
    }

    [Fact]
    public void TryValidateDecimalPrecision_ShouldReturnFalse_WhenPrecisionIsNegative()
    {
        var result = AccountingSettingsAggregate.TryValidateDecimalPrecision(-1, out var error);

        result.ShouldBeFalse();
        error.ShouldNotBeEmpty();
    }

    [Fact]
    public void TryValidateDecimalPrecision_ShouldReturnFalse_WhenPrecisionExceedsMax()
    {
        var result = AccountingSettingsAggregate.TryValidateDecimalPrecision(5, out var error);

        result.ShouldBeFalse();
        error.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData(-100)]
    [InlineData(-1)]
    [InlineData(5)]
    [InlineData(100)]
    public void TryValidateDecimalPrecision_ShouldReturnFalse_ForOutOfRangeValues(int precision)
    {
        var result = AccountingSettingsAggregate.TryValidateDecimalPrecision(precision, out var error);

        result.ShouldBeFalse();
        error.ShouldNotBeEmpty();
    }

    // --- TryValidateThousandSeparator ---

    [Theory]
    [InlineData(",")]
    [InlineData(" ")]
    [InlineData("'")]
    public void TryValidateThousandSeparator_ShouldReturnTrue_ForValidSingleCharacter(string separator)
    {
        var result = AccountingSettingsAggregate.TryValidateThousandSeparator(separator, out var error);

        result.ShouldBeTrue();
        error.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("12")]
    [InlineData("ab")]
    [InlineData("1")]
    public void TryValidateThousandSeparator_ShouldReturnFalse_ForInvalidSeparator(string separator)
    {
        var result = AccountingSettingsAggregate.TryValidateThousandSeparator(separator, out var error);

        result.ShouldBeFalse();
        error.ShouldNotBeEmpty();
    }

    // --- TryValidateAvailableCurrencies ---

    [Fact]
    public void TryValidateAvailableCurrencies_ShouldReturnTrue_ForNonEmptyValidList()
    {
        var result = AccountingSettingsAggregate.TryValidateAvailableCurrencies(
        [
            new Currency("USD", "$"),
            new Currency("kes", "KSh"),
            new Currency("EUR", "EUR")
        ], out var error);

        result.ShouldBeTrue();
        error.ShouldBeEmpty();
    }

    [Fact]
    public void TryValidateAvailableCurrencies_ShouldReturnFalse_WhenListIsEmpty()
    {
        var result = AccountingSettingsAggregate.TryValidateAvailableCurrencies([], out var error);

        result.ShouldBeFalse();
        error.ShouldNotBeEmpty();
    }

    [Fact]
    public void TryValidateAvailableCurrencies_ShouldReturnFalse_WhenAnyCurrencyIsInvalid()
    {
        var result = AccountingSettingsAggregate.TryValidateAvailableCurrencies(
        [
            new Currency("USD", "$"),
            new Currency(" ", "")
        ], out var error);

        result.ShouldBeFalse();
        error.ShouldNotBeEmpty();
    }

    [Fact]
    public void NormalizeCurrencies_ShouldReturnDistinctUppercaseSortedValues()
    {
        var normalized = AccountingSettingsAggregate.NormalizeCurrencies(
        [
            new Currency("kes", "KSh"),
            new Currency("USD", "$"),
            new Currency(" usd ", "US$"),
            new Currency("EUR", "EUR")
        ]);

        normalized.Select(currency => currency.Code).ShouldBe(["EUR", "KES", "USD"]);
        normalized.First(currency => currency.Code == "USD").Symbol.ShouldBe("$");
    }
}
