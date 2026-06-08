using System.Globalization;
using Jamaa.Desktop.Accounting;
using Shouldly;
using Xunit;

namespace UnitTests.Accounting;

public class AccountItemViewModelTests
{
    [Fact]
    public void OpeningBalanceText_SettingValidTextUpdatesDecimalValue()
    {
        var viewModel = new AccountItemViewModel();

        viewModel.OpeningBalanceText = "1234.56";

        viewModel.OpeningBalance.ShouldBe(1234.56m);
    }

    [Fact]
    public void OpeningBalanceText_SettingGroupedTextUpdatesDecimalValue()
    {
        var viewModel = new AccountItemViewModel();
        var previousCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

            viewModel.OpeningBalanceText = "1'234,56";

            viewModel.OpeningBalance.ShouldBe(1234.56m);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void ForceFormatOpeningBalance_UpdatesTextFromDecimalValue()
    {
        var viewModel = new AccountItemViewModel
        {
            DecimalPrecision = 3,
            ThousandSeparator = "'",
            OpeningBalance = 1234.56m
        };

        viewModel.ForceFormatOpeningBalance();

        var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        viewModel.OpeningBalanceText.ShouldBe($"1'234{decimalSeparator}560");
    }

    [Fact]
    public void ContraAccountDisplay_ReflectsContraState()
    {
        var viewModel = new AccountItemViewModel { IsContraAccount = true };

        viewModel.ContraAccountDisplay.ShouldBe("Yes");
    }
}
