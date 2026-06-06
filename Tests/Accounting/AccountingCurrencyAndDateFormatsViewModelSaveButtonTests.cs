using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Domain.Accounting.Values;
using Jamaa.Application.Accounting;
using Jamaa.Application.Accounting.Models;
using Jamaa.Application.Users;
using Jamaa.Application.Users.Services;
using Jamaa.Data.Models.Organisation;
using Jamaa.Desktop.Accounting;
using Jamaa.Desktop.Services.Notifications;
using NSubstitute;
using Shouldly;
using Xunit;

namespace UnitTests.Accounting;

/// <summary>
///     Focused unit tests for the Save button state in <see cref="AccountingCurrencyAndDateFormatsViewModel" />.
///     Tests cover:
///     - Disabled when no changes have been made
///     - Enabled when valid changes are made
///     - Disabled again after successful persistence is confirmed
///     - Error status when save fails
///     - Disabled while save is in-flight
/// </summary>
public class AccountingCurrencyAndDateFormatsViewModelSaveButtonTests : IDisposable
{
    private const string OrgId = "org-save-button-test";
    private readonly BehaviorSubject<AccountingSettingsData?> _currentSettingsSubject;

    private readonly IAccountingFacade _facade;
    private readonly INotificationService _notificationService;
    private readonly Subject<AccountingSettingsData> _settingsUpdatedSubject;
    private readonly AccountingCurrencyAndDateFormatsViewModel _vm;

    public AccountingCurrencyAndDateFormatsViewModelSaveButtonTests()
    {
        _currentSettingsSubject = new BehaviorSubject<AccountingSettingsData?>(null);
        _settingsUpdatedSubject = new Subject<AccountingSettingsData>();

        _facade = Substitute.For<IAccountingFacade>();
        _notificationService = Substitute.For<INotificationService>();
        _facade.CurrentAccountingSettings.Returns(_currentSettingsSubject.AsObservable());
        _facade.AccountingSettingsUpdated.Returns(_settingsUpdatedSubject.AsObservable());
        _facade.UpdateAccountingSettings(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<Currency>>())
            .Returns(Task.CompletedTask);

        var org = new OrganisationData { Id = OrgId, Name = "Test Org" };
        var session = new UserSession(true, "admin", Guid.NewGuid(), org);
        var userSessionService = Substitute.For<IUserSessionService>();
        userSessionService.CurrentUserSession.Returns(session);

        _vm = new AccountingCurrencyAndDateFormatsViewModel(_facade, userSessionService, _notificationService);
    }

    public void Dispose()
    {
        _vm.Dispose();
        _currentSettingsSubject.Dispose();
        _settingsUpdatedSubject.Dispose();
    }

    // --------------------------------------------------------------------------
    // Helpers
    // --------------------------------------------------------------------------

    /// <summary>Pushes a settings snapshot as if the read-model stream has loaded saved state.</summary>
    private async Task PushPersistedSettings(
        string currency = "USD",
        string dateFormat = "DD/MM/YYYY",
        int precision = 2,
        string thousandSeparator = ",")
    {
        var availableCurrencies = new[] { "USD", "KES", "EUR", "GBP", "ZAR", "NGN" };
        var symbols = new Dictionary<string, string>
        {
            ["USD"] = "$",
            ["KES"] = "KSh",
            ["EUR"] = "EUR",
            ["GBP"] = "GBP",
            ["ZAR"] = "R",
            ["NGN"] = "N"
        };
        _currentSettingsSubject.OnNext(new AccountingSettingsData
        {
            OrganisationId = OrgId,
            BaseCurrency = currency,
            DateFormat = dateFormat,
            DecimalPrecision = precision,
            ThousandSeparator = thousandSeparator,
            AvailableCurrencies = availableCurrencies
                .Select(code => new AccountingAvailableCurrencyData
                {
                    OrganisationId = OrgId,
                    CurrencyCode = code,
                    CurrencySymbol = symbols[code]
                })
                .ToList()
        });

        // Allow the ObserveOn(SynchronizationContext) callback to execute on the thread pool.
        await Task.Delay(100);
    }

    /// <summary>
    ///     Simulates the OrganisationProjection confirming successful persistence by emitting
    ///     the updated settings on the AccountingSettingsUpdated stream.
    /// </summary>
    private async Task ConfirmPersistenceFromStream(
        string currency,
        string dateFormat,
        int precision,
        string thousandSeparator = ",")
    {
        // Small initial delay so the in-flight save has had time to register its TCS.
        await Task.Delay(50);

        _settingsUpdatedSubject.OnNext(new AccountingSettingsData
        {
            OrganisationId = OrgId,
            BaseCurrency = currency,
            DateFormat = dateFormat,
            DecimalPrecision = precision,
            ThousandSeparator = thousandSeparator,
            AvailableCurrencies = _vm.AvailableCurrencies
                .Select(currencyData => new AccountingAvailableCurrencyData
                {
                    OrganisationId = OrgId,
                    CurrencyCode = currencyData.CurrencyCode,
                    CurrencySymbol = currencyData.CurrencySymbol
                })
                .ToList()
        });

        // Allow the ObserveOn callback on the thread pool to run and call TrySetResult.
        await Task.Delay(100);
    }

    // --------------------------------------------------------------------------
    // Tests: No changes → Save disabled
    // --------------------------------------------------------------------------

    [Fact]
    public async Task SaveButton_IsDisabled_WhenNoSettingsHaveBeenLoaded()
    {
        // No push to _currentSettingsSubject yet → _hasPersistedSnapshot is false
        _vm.SaveBaseSettingsCommand.CanExecute(null).ShouldBeFalse();
        _vm.SaveAvailableCurrenciesCommand.CanExecute(null).ShouldBeFalse();

        await Task.CompletedTask; // suppress async warning
    }

    [Fact]
    public async Task SaveButton_IsDisabled_AfterSettingsLoad_WithoutAnyChanges()
    {
        await PushPersistedSettings();

        // Selections are identical to persisted snapshot → no unsaved changes
        _vm.SaveBaseSettingsCommand.CanExecute(null).ShouldBeFalse();
        _vm.SaveAvailableCurrenciesCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public async Task SaveButton_IsDisabled_WhenUserClearsChangeBackToOriginalValue()
    {
        await PushPersistedSettings();

        _vm.SelectedBaseCurrency = "KES"; // change
        _vm.SaveBaseSettingsCommand.CanExecute(null).ShouldBeTrue();

        _vm.SelectedBaseCurrency = "USD"; // revert
        _vm.SaveBaseSettingsCommand.CanExecute(null).ShouldBeFalse();
    }

    // --------------------------------------------------------------------------
    // Tests: Valid changes → Save enabled
    // --------------------------------------------------------------------------

    [Fact]
    public async Task SaveButton_IsEnabled_WhenBaseCurrencyChangedToValidValue()
    {
        await PushPersistedSettings();

        _vm.SelectedBaseCurrency = "KES";

        _vm.SaveBaseSettingsCommand.CanExecute(null).ShouldBeTrue();
    }

    [Fact]
    public async Task SaveButton_IsEnabled_WhenDateFormatChangedToValidValue()
    {
        await PushPersistedSettings();

        _vm.SelectedDateFormat = "MM/DD/YYYY";

        _vm.SaveBaseSettingsCommand.CanExecute(null).ShouldBeTrue();
    }

    [Fact]
    public async Task SaveButton_IsEnabled_WhenDecimalPrecisionChangedToValidValue()
    {
        await PushPersistedSettings();

        _vm.SelectedDecimalPrecision = 4;

        _vm.SaveBaseSettingsCommand.CanExecute(null).ShouldBeTrue();
    }

    [Fact]
    public async Task SaveCurrencyButton_IsEnabled_WhenAvailableCurrencyIsAdded()
    {
        await PushPersistedSettings();

        _vm.NewCurrencyCode = "TZS";
        _vm.NewCurrencySymbol = "TSh";
        _vm.AddCurrencyCommand.Execute(null);

        _vm.AvailableCurrencies.Any(c => c.CurrencyCode == "TZS").ShouldBeTrue();
        _vm.SaveAvailableCurrenciesCommand.CanExecute(null).ShouldBeTrue();
    }

    [Fact]
    public async Task BaseCurrency_RemainsSelected_WhenAvailableCurrenciesChange()
    {
        await PushPersistedSettings();
        _vm.SelectedBaseCurrency = "KES";

        _vm.NewCurrencyCode = "TZS";
        _vm.NewCurrencySymbol = "TSh";
        _vm.AddCurrencyCommand.Execute(null);

        _vm.SelectedBaseCurrency.ShouldBe("KES");
        _vm.AvailableCurrencies.Any(c => c.CurrencyCode == "KES").ShouldBeTrue();
    }

    [Fact]
    public async Task AddingCurrency_ClearsExistingCurrencyErrorMessage()
    {
        await PushPersistedSettings();

        // Create a currency-section error first.
        // We need to use a scenario where Remove is allowed but might fail for other reasons, 
        // OR manually set the error status to test the clear logic.
        // In the current VM, RemoveSelectedCurrency only sets CurrencyStatusMessage if Count <= 1.
        
        // Let's manually trigger an error state to verify it gets cleared on Add.
        _vm.GetType().GetProperty("HasCurrencyErrorStatus")?.SetValue(_vm, true);
        _vm.CurrencyStatusMessage = "Some error";

        // Successful add should clear the stale error banner.
        _vm.NewCurrencyCode = "TZS";
        _vm.NewCurrencySymbol = "TSh";
        _vm.AddCurrencyCommand.Execute(null);

        _vm.AvailableCurrencies.Any(c => c.CurrencyCode == "TZS").ShouldBeTrue();
        _vm.HasCurrencyErrorStatus.ShouldBeFalse();
        _vm.CurrencyStatusMessage.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task RemovingBaseCurrency_IsCommandDisabled()
    {
        await PushPersistedSettings();
        _vm.SelectedAvailableCurrencyCode = "USD";
        _vm.SelectedBaseCurrency = "USD";

        _vm.RemoveSelectedCurrencyCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public async Task RemovingNonBaseCurrency_IsCommandEnabled()
    {
        await PushPersistedSettings();
        _vm.SelectedAvailableCurrencyCode = "KES";
        _vm.SelectedBaseCurrency = "USD";

        _vm.RemoveSelectedCurrencyCommand.CanExecute(null).ShouldBeTrue();
    }

    [Fact]
    public async Task SaveCurrencyButton_IsEnabled_WhenFirstCurrencyIsAdded_WithoutPersistedSnapshot()
    {
        _vm.SaveAvailableCurrenciesCommand.CanExecute(null).ShouldBeFalse();

        _vm.NewCurrencyCode = "KES";
        _vm.NewCurrencySymbol = "KSh";
        _vm.AddCurrencyCommand.Execute(null);

        _vm.AvailableCurrencies.Select(c => c.CurrencyCode).ShouldContain("KES");
        _vm.SelectedBaseCurrency.ShouldBe("KES");
        _vm.IsSelectionValid.ShouldBeTrue();
        _vm.SaveAvailableCurrenciesCommand.CanExecute(null).ShouldBeTrue();

        await Task.CompletedTask;
    }

    [Fact]
    public async Task AddingFirstCurrency_ShouldReplaceInvalidDefaultBaseCurrency()
    {
        _vm.SelectedBaseCurrency.ShouldBe("USD");

        _vm.NewCurrencyCode = "UGX";
        _vm.NewCurrencySymbol = "USh";
        _vm.AddCurrencyCommand.Execute(null);

        _vm.AvailableCurrencies.Count.ShouldBe(1);
        _vm.AvailableCurrencies[0].CurrencyCode.ShouldBe("UGX");
        _vm.SelectedBaseCurrency.ShouldBe("UGX");
        _vm.IsSelectionValid.ShouldBeTrue();

        await Task.CompletedTask;
    }

    // --------------------------------------------------------------------------
    // Tests: Successful persistence → Save re-disabled
    // --------------------------------------------------------------------------

    [Fact]
    public async Task SaveButton_IsDisabled_AfterSuccessfulPersistence()
    {
        await PushPersistedSettings();
        _vm.SelectedBaseCurrency = "EUR";

        _vm.SaveBaseSettingsCommand.CanExecute(null).ShouldBeTrue();

        var saveTask = _vm.SaveBaseSettingsCommand.ExecuteAsync(null);
        await ConfirmPersistenceFromStream("EUR", "DD/MM/YYYY", 2);
        await saveTask;

        _vm.SaveBaseSettingsCommand.CanExecute(null).ShouldBeFalse();
        _vm.IsOperationInFlight.ShouldBeFalse();
        _vm.HasErrorStatus.ShouldBeFalse();
    }

    [Fact]
    public async Task SuccessNotification_IsShown_AfterPersistenceConfirmed()
    {
        await PushPersistedSettings();
        _vm.SelectedDateFormat = "YYYY-MM-DD";

        var saveTask = _vm.SaveBaseSettingsCommand.ExecuteAsync(null);
        await ConfirmPersistenceFromStream("USD", "YYYY-MM-DD", 2);
        await saveTask;

        _notificationService.Received(1).Show(
            "Accounting settings",
            "Saved successfully.",
            NotificationType.Success);
    }

    [Fact]
    public async Task BaseCurrency_Recovers_WhenSelectionIsClearedByUi()
    {
        await PushPersistedSettings("KES");

        // Simulates transient ComboBox reset during item-source refresh.
        _vm.SelectedBaseCurrency = string.Empty;

        _vm.SelectedBaseCurrency.ShouldBe("KES");
        _vm.IsSelectionValid.ShouldBeTrue();
    }

    // --------------------------------------------------------------------------
    // Tests: Save in-flight → disabled during operation
    // --------------------------------------------------------------------------

    [Fact]
    public async Task SaveButton_IsDisabled_WhileSaveIsInFlight()
    {
        await PushPersistedSettings();
        _vm.SelectedBaseCurrency = "GBP";

        // Start the save but don't confirm persistence yet
        var saveTask = _vm.SaveBaseSettingsCommand.ExecuteAsync(null);
        await Task.Delay(50); // give SaveBaseSettings time to set IsOperationInFlight = true

        _vm.IsOperationInFlight.ShouldBeTrue();
        _vm.SaveBaseSettingsCommand.CanExecute(null).ShouldBeFalse();

        // Unblock the task
        await ConfirmPersistenceFromStream("GBP", "DD/MM/YYYY", 2);
        await saveTask;
    }

    // --------------------------------------------------------------------------
    // Tests: Save fails → error status displayed
    // --------------------------------------------------------------------------

    [Fact]
    public async Task StatusMessage_ContainsErrorDetail_WhenFacadeThrowsOnUpdate()
    {
        await PushPersistedSettings();
        _vm.SelectedBaseCurrency = "ZAR";

        _facade.UpdateAccountingSettings(OrgId, "ZAR", "DD/MM/YYYY", 2, ",", Arg.Any<IReadOnlyList<Currency>>())
            .Returns(Task.FromException(new InvalidOperationException("Simulated dispatch failure")));

        await _vm.SaveBaseSettingsCommand.ExecuteAsync(null);

        _vm.HasErrorStatus.ShouldBeTrue();
        _vm.IsOperationInFlight.ShouldBeFalse();

        _notificationService.Received(1).Show(
            "Error",
            Arg.Is<string>(message => message.Contains("Failed to saved", StringComparison.OrdinalIgnoreCase)),
            NotificationType.Error);
    }

    [Fact]
    public async Task SaveButton_IsEnabledAgain_AfterFailedSave_IfChangesRemain()
    {
        await PushPersistedSettings();
        _vm.SelectedBaseCurrency = "NGN";

        _facade.UpdateAccountingSettings(OrgId, "NGN", "DD/MM/YYYY", 2, ",", Arg.Any<IReadOnlyList<Currency>>())
            .Returns(Task.FromException(new InvalidOperationException("Transient error")));

        await _vm.SaveBaseSettingsCommand.ExecuteAsync(null);

        // After a failed save the persisted snapshot didn't change,
        // so the UI still has unsaved changes → Save should be re-enabled.
        _vm.SaveBaseSettingsCommand.CanExecute(null).ShouldBeTrue();
    }

    [Fact]
    public async Task HasErrorStatus_IsFalse_WhenSaveSucceedsAfterAPreviousFailure()
    {
        await PushPersistedSettings();
        _vm.SelectedBaseCurrency = "GBP";

        // First attempt fails
        _facade.UpdateAccountingSettings(OrgId, "GBP", "DD/MM/YYYY", 2, ",", Arg.Any<IReadOnlyList<Currency>>())
            .Returns(Task.FromException(new InvalidOperationException("First failure")));
        await _vm.SaveBaseSettingsCommand.ExecuteAsync(null);
        _vm.HasErrorStatus.ShouldBeTrue();

        // Second attempt succeeds
        _facade.UpdateAccountingSettings(OrgId, "GBP", "DD/MM/YYYY", 2, ",", Arg.Any<IReadOnlyList<Currency>>())
            .Returns(Task.CompletedTask);
        var saveTask = _vm.SaveBaseSettingsCommand.ExecuteAsync(null);
        await ConfirmPersistenceFromStream("GBP", "DD/MM/YYYY", 2);
        await saveTask;

        _vm.HasErrorStatus.ShouldBeFalse();

        _notificationService.Received().Show(
            "Accounting settings",
            "Saved successfully.",
            NotificationType.Success);
    }
}
