using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Jamaa.Application.Finances;
using Jamaa.Application.Finances.Values;
using Jamaa.Application.Users;
using Jamaa.Application.Users.Services;
using Jamaa.Data.Models.Finances;
using Jamaa.Data.Models.Organisation;
using Jamaa.Desktop.Accounting;
using NSubstitute;
using Shouldly;
using Xunit;

namespace UnitTests.Accounting;

/// <summary>
/// Focused unit tests for the Save button state in <see cref="AccountingCurrencyAndDateFormatsViewModel"/>.
/// Tests cover:
///   - Disabled when no changes have been made
///   - Enabled when valid changes are made
///   - Disabled again after successful persistence is confirmed
///   - Error status when save fails
///   - Disabled while save is in-flight
/// </summary>
public class AccountingCurrencyAndDateFormatsViewModelSaveButtonTests : IDisposable
{
    private const string OrgId = "org-save-button-test";

    private readonly IFinanceManagementFacade _facade;
    private readonly BehaviorSubject<AccountingSettingsData?> _currentSettingsSubject;
    private readonly Subject<AccountingSettingsData> _settingsUpdatedSubject;
    private readonly AccountingCurrencyAndDateFormatsViewModel _vm;

    public AccountingCurrencyAndDateFormatsViewModelSaveButtonTests()
    {
        _currentSettingsSubject = new BehaviorSubject<AccountingSettingsData?>(null);
        _settingsUpdatedSubject = new Subject<AccountingSettingsData>();

        _facade = Substitute.For<IFinanceManagementFacade>();
        _facade.CurrentAccountingSettings.Returns(_currentSettingsSubject.AsObservable());
        _facade.AccountingSettingsUpdated.Returns(_settingsUpdatedSubject.AsObservable());
        _facade.UpdateAccountingSettings(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<IReadOnlyList<Currency>>())
            .Returns(Task.CompletedTask);

        var org = new OrganisationData { Id = OrgId, Name = "Test Org" };
        var session = new UserSession(true, "admin", Guid.NewGuid(), org);
        var userSessionService = Substitute.For<IUserSessionService>();
        userSessionService.CurrentUserSession.Returns(session);

        _vm = new AccountingCurrencyAndDateFormatsViewModel(_facade, userSessionService);
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
        int precision = 2)
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
    /// Simulates the OrganisationProjection confirming successful persistence by emitting
    /// the updated settings on the AccountingSettingsUpdated stream.
    /// </summary>
    private async Task ConfirmPersistenceFromStream(
        string currency,
        string dateFormat,
        int precision)
    {
        // Small initial delay so the in-flight save has had time to register its TCS.
        await Task.Delay(50);

        _settingsUpdatedSubject.OnNext(new AccountingSettingsData
        {
            OrganisationId = OrgId,
            BaseCurrency = currency,
            DateFormat = dateFormat,
            DecimalPrecision = precision,
            AvailableCurrencies = new[] { "USD", "KES", "EUR", "GBP", "ZAR", "NGN" }
                .Select(code => new AccountingAvailableCurrencyData
                {
                    OrganisationId = OrgId,
                    CurrencyCode = code,
                    CurrencySymbol = code
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
        _vm.SaveSettingsCommand.CanExecute(null).ShouldBeFalse();

        await Task.CompletedTask; // suppress async warning
    }

    [Fact]
    public async Task SaveButton_IsDisabled_AfterSettingsLoad_WithoutAnyChanges()
    {
        await PushPersistedSettings("USD", "DD/MM/YYYY", 2);

        // Selections are identical to persisted snapshot → no unsaved changes
        _vm.SaveSettingsCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public async Task SaveButton_IsDisabled_WhenUserClearsChangeBackToOriginalValue()
    {
        await PushPersistedSettings("USD", "DD/MM/YYYY", 2);

        _vm.SelectedBaseCurrency = "KES"; // change
        _vm.SaveSettingsCommand.CanExecute(null).ShouldBeTrue();

        _vm.SelectedBaseCurrency = "USD"; // revert
        _vm.SaveSettingsCommand.CanExecute(null).ShouldBeFalse();
    }

    // --------------------------------------------------------------------------
    // Tests: Valid changes → Save enabled
    // --------------------------------------------------------------------------

    [Fact]
    public async Task SaveButton_IsEnabled_WhenBaseCurrencyChangedToValidValue()
    {
        await PushPersistedSettings("USD", "DD/MM/YYYY", 2);

        _vm.SelectedBaseCurrency = "KES";

        _vm.SaveSettingsCommand.CanExecute(null).ShouldBeTrue();
    }

    [Fact]
    public async Task SaveButton_IsEnabled_WhenDateFormatChangedToValidValue()
    {
        await PushPersistedSettings("USD", "DD/MM/YYYY", 2);

        _vm.SelectedDateFormat = "MM/DD/YYYY";

        _vm.SaveSettingsCommand.CanExecute(null).ShouldBeTrue();
    }

    [Fact]
    public async Task SaveButton_IsEnabled_WhenDecimalPrecisionChangedToValidValue()
    {
        await PushPersistedSettings("USD", "DD/MM/YYYY", 2);

        _vm.SelectedDecimalPrecision = 4;

        _vm.SaveSettingsCommand.CanExecute(null).ShouldBeTrue();
    }

    [Fact]
    public async Task SaveButton_IsEnabled_WhenAvailableCurrencyIsAdded()
    {
        await PushPersistedSettings("USD", "DD/MM/YYYY", 2);

        _vm.NewCurrencyCode = "TZS";
        _vm.NewCurrencySymbol = "TSh";
        _vm.AddCurrencyCommand.Execute(null);

        _vm.AvailableCurrencies.Any(c => c.CurrencyCode == "TZS").ShouldBeTrue();
        _vm.SaveSettingsCommand.CanExecute(null).ShouldBeTrue();
    }

    [Fact]
    public async Task RemovingBaseCurrency_SelectsAnotherCurrencyAndKeepsStateValid()
    {
        await PushPersistedSettings("USD", "DD/MM/YYYY", 2);
        _vm.SelectedAvailableCurrencyCode = "USD";

        _vm.RemoveSelectedCurrencyCommand.Execute(null);

        _vm.SelectedBaseCurrency.ShouldNotBe("USD");
        _vm.IsSelectionValid.ShouldBeTrue();
        _vm.SaveSettingsCommand.CanExecute(null).ShouldBeTrue();
    }

    [Fact]
    public async Task SaveButton_IsEnabled_WhenFirstCurrencyIsAdded_WithoutPersistedSnapshot()
    {
        _vm.SaveSettingsCommand.CanExecute(null).ShouldBeFalse();

        _vm.NewCurrencyCode = "KES";
        _vm.NewCurrencySymbol = "KSh";
        _vm.AddCurrencyCommand.Execute(null);

        _vm.AvailableCurrencies.Select(c => c.CurrencyCode).ShouldContain("KES");
        _vm.SelectedBaseCurrency.ShouldBe("KES");
        _vm.IsSelectionValid.ShouldBeTrue();
        _vm.SaveSettingsCommand.CanExecute(null).ShouldBeTrue();

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
        await PushPersistedSettings("USD", "DD/MM/YYYY", 2);
        _vm.SelectedBaseCurrency = "EUR";

        _vm.SaveSettingsCommand.CanExecute(null).ShouldBeTrue();

        var saveTask = _vm.SaveSettingsCommand.ExecuteAsync(null);
        await ConfirmPersistenceFromStream("EUR", "DD/MM/YYYY", 2);
        await saveTask;

        _vm.SaveSettingsCommand.CanExecute(null).ShouldBeFalse();
        _vm.IsSaving.ShouldBeFalse();
        _vm.IsAwaitingPersistenceConfirmation.ShouldBeFalse();
        _vm.HasErrorStatus.ShouldBeFalse();
    }

    [Fact]
    public async Task StatusMessage_IsSuccess_AfterPersistenceConfirmed()
    {
        await PushPersistedSettings("USD", "DD/MM/YYYY", 2);
        _vm.SelectedDateFormat = "YYYY-MM-DD";

        var saveTask = _vm.SaveSettingsCommand.ExecuteAsync(null);
        await ConfirmPersistenceFromStream("USD", "YYYY-MM-DD", 2);
        await saveTask;

        _vm.StatusMessage.ShouldBe("Settings saved successfully.");
        _vm.HasStatusMessage.ShouldBeTrue();
    }

    // --------------------------------------------------------------------------
    // Tests: Save in-flight → disabled during operation
    // --------------------------------------------------------------------------

    [Fact]
    public async Task SaveButton_IsDisabled_WhileSaveIsInFlight()
    {
        await PushPersistedSettings("USD", "DD/MM/YYYY", 2);
        _vm.SelectedBaseCurrency = "GBP";

        // Start the save but don't confirm persistence yet
        var saveTask = _vm.SaveSettingsCommand.ExecuteAsync(null);
        await Task.Delay(50); // give SaveSettings time to set IsSaving = true

        _vm.IsSaving.ShouldBeTrue();
        _vm.SaveSettingsCommand.CanExecute(null).ShouldBeFalse();

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
        await PushPersistedSettings("USD", "DD/MM/YYYY", 2);
        _vm.SelectedBaseCurrency = "ZAR";

        _facade.UpdateAccountingSettings(OrgId, "ZAR", "DD/MM/YYYY", 2, Arg.Any<IReadOnlyList<Currency>>())
            .Returns(Task.FromException(new InvalidOperationException("Simulated dispatch failure")));

        await _vm.SaveSettingsCommand.ExecuteAsync(null);

        _vm.HasErrorStatus.ShouldBeTrue();
        _vm.StatusMessage.ShouldContain("Simulated dispatch failure");
        _vm.IsSaving.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveButton_IsEnabledAgain_AfterFailedSave_IfChangesRemain()
    {
        await PushPersistedSettings("USD", "DD/MM/YYYY", 2);
        _vm.SelectedBaseCurrency = "NGN";

        _facade.UpdateAccountingSettings(OrgId, "NGN", "DD/MM/YYYY", 2, Arg.Any<IReadOnlyList<Currency>>())
            .Returns(Task.FromException(new InvalidOperationException("Transient error")));

        await _vm.SaveSettingsCommand.ExecuteAsync(null);

        // After a failed save the persisted snapshot didn't change,
        // so the UI still has unsaved changes → Save should be re-enabled.
        _vm.SaveSettingsCommand.CanExecute(null).ShouldBeTrue();
    }

    [Fact]
    public async Task HasErrorStatus_IsFalse_WhenSaveSucceedsAfterAPreviousFailure()
    {
        await PushPersistedSettings("USD", "DD/MM/YYYY", 2);
        _vm.SelectedBaseCurrency = "GBP";

        // First attempt fails
        _facade.UpdateAccountingSettings(OrgId, "GBP", "DD/MM/YYYY", 2, Arg.Any<IReadOnlyList<Currency>>())
            .Returns(Task.FromException(new InvalidOperationException("First failure")));
        await _vm.SaveSettingsCommand.ExecuteAsync(null);
        _vm.HasErrorStatus.ShouldBeTrue();

        // Second attempt succeeds
        _facade.UpdateAccountingSettings(OrgId, "GBP", "DD/MM/YYYY", 2, Arg.Any<IReadOnlyList<Currency>>())
            .Returns(Task.CompletedTask);
        var saveTask = _vm.SaveSettingsCommand.ExecuteAsync(null);
        await ConfirmPersistenceFromStream("GBP", "DD/MM/YYYY", 2);
        await saveTask;

        _vm.HasErrorStatus.ShouldBeFalse();
        _vm.StatusMessage.ShouldBe("Settings saved successfully.");
    }
}

