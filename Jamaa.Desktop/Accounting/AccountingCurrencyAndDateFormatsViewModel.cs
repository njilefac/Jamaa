using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jamaa.Application.Finances;
using Jamaa.Application.Finances.Values;
using Jamaa.Application.Users.Services;
using Jamaa.Data.Models.Finances;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting;

public partial class AccountingCurrencyAndDateFormatsViewModel : ObservableObject, IApplicationModule, IRouteableViewModel, IDisposable
{
    private readonly IFinanceManagementFacade _financeManagementFacade;
    private readonly string _organisationId;
    private readonly CompositeDisposable _subscriptions = [];
    private string _persistedBaseCurrency = "USD";
    private string _persistedDateFormat = "DD/MM/YYYY";
    private int _persistedDecimalPrecision = 2;
    private IReadOnlyList<string> _persistedAvailableCurrencySnapshotKeys = ["EUR|EUR", "KES|KSh", "USD|$"];
    private bool _hasPersistedSnapshot;
    private TaskCompletionSource? _pendingSaveConfirmation;

    public AccountingCurrencyAndDateFormatsViewModel(
        IFinanceManagementFacade financeManagementFacade,
        IUserSessionService userSessionService)
    {
        var syncContext = SynchronizationContext.Current ?? new SynchronizationContext();

        _financeManagementFacade = financeManagementFacade;
        _organisationId = ResolveOrganisationId(userSessionService);

        _subscriptions.Add(
            financeManagementFacade.CurrentAccountingSettings
                .ObserveOn(syncContext)
                .Subscribe(ApplySettings, HandleStreamError));

        _subscriptions.Add(
            financeManagementFacade.AccountingSettingsUpdated
                .Where(settings => settings.OrganisationId == _organisationId)
                .ObserveOn(syncContext)
                .Subscribe(updated =>
                {
                    ApplySettings(updated);
                    _pendingSaveConfirmation?.TrySetResult();
                    IsAwaitingPersistenceConfirmation = false;
                    HasErrorStatus = false;
                    StatusMessage = "Settings saved successfully.";
                }, HandleStreamError));
    }

    public Guid Id => Guid.Parse("d3a693eb-5b4c-44ee-ae45-cf32b5ec4fe9");
    public string Title => "Currency & Date Formats";
    public object? HeaderContent => null;

    public ObservableCollection<AccountingAvailableCurrencyData> AvailableCurrencies { get; } = [];
    public IReadOnlyList<AccountingAvailableCurrencyData> BaseCurrencyOptions => AvailableCurrencies;
    public IReadOnlyList<string> DateFormatOptions { get; } = ["DD/MM/YYYY", "MM/DD/YYYY", "YYYY-MM-DD"];
    public IReadOnlyList<int> DecimalPrecisionOptions { get; } = [0, 1, 2, 3, 4];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattingPreview))]
    private string _selectedBaseCurrency = "USD";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattingPreview))]
    private string _selectedDateFormat = "DD/MM/YYYY";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattingPreview))]
    private int _selectedDecimalPrecision = 2;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveSettingsCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveSelectedCurrencyCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddCurrencyCommand))]
    private bool _isSaving;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveSettingsCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveSelectedCurrencyCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddCurrencyCommand))]
    private bool _isAwaitingPersistenceConfirmation;

    [ObservableProperty]
    private bool _hasErrorStatus;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCurrencyCommand))]
    private string _newCurrencyCode = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCurrencyCommand))]
    private string _newCurrencySymbol = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveSelectedCurrencyCommand))]
    private string _selectedAvailableCurrencyCode = string.Empty;

    public bool HasUnsavedChanges =>
        _hasPersistedSnapshot
            ? SelectedBaseCurrency != _persistedBaseCurrency ||
              SelectedDateFormat != _persistedDateFormat ||
              SelectedDecimalPrecision != _persistedDecimalPrecision ||
              !BuildAvailableCurrencySnapshotKeys(AvailableCurrencies).SequenceEqual(_persistedAvailableCurrencySnapshotKeys)
            : SelectedBaseCurrency != _persistedBaseCurrency ||
              SelectedDateFormat != _persistedDateFormat ||
              SelectedDecimalPrecision != _persistedDecimalPrecision ||
              AvailableCurrencies.Count > 0;

    public bool IsSelectionValid =>
        AvailableCurrencies.Count > 0 &&
        BaseCurrencyOptions.Any(option => option.CurrencyCode == SelectedBaseCurrency) &&
        DateFormatOptions.Any(option => option == SelectedDateFormat) &&
        DecimalPrecisionOptions.Any(option => option == SelectedDecimalPrecision) &&
        AvailableCurrencies.All(IsValidCurrency);

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public string FormattingPreview
    {
        get
        {
            var exampleDate = new DateTime(2026, 4, 24).ToString(ResolveDotNetDateFormat(SelectedDateFormat), CultureInfo.InvariantCulture);
            var exampleAmount = 1234567.0m.ToString($"N{SelectedDecimalPrecision}", CultureInfo.InvariantCulture);
            var symbol = ResolveBaseCurrencySymbol();
            return $"{symbol} {exampleAmount}   |   {exampleDate}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveSettings))]
    private async Task SaveSettings()
    {
        if (!CanSaveSettings())
        {
            return;
        }

        IsSaving = true;
        IsAwaitingPersistenceConfirmation = true;
        HasErrorStatus = false;
        StatusMessage = "Saving settings...";

        _pendingSaveConfirmation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var availableCurrencies = AvailableCurrencies
                .Select(currency => new Currency(currency.CurrencyCode, currency.CurrencySymbol))
                .ToList();

            await _financeManagementFacade.UpdateAccountingSettings(
                _organisationId,
                SelectedBaseCurrency,
                SelectedDateFormat,
                SelectedDecimalPrecision,
                availableCurrencies);

            await WaitForSaveConfirmation(_pendingSaveConfirmation);
        }
        catch (Exception ex)
        {
            IsAwaitingPersistenceConfirmation = false;
            HasErrorStatus = true;
            StatusMessage = $"Failed to save settings: {ex.Message}";
        }
        finally
        {
            _pendingSaveConfirmation = null;
            IsSaving = false;
        }
    }

    private bool CanSaveSettings()
    {
        return !IsSaving
               && !IsAwaitingPersistenceConfirmation
               && IsSelectionValid
               && HasUnsavedChanges;
    }

    [RelayCommand(CanExecute = nameof(CanAddCurrency))]
    private void AddCurrency()
    {
        var normalizedCode = NormalizeCurrencyCode(NewCurrencyCode);
        var normalizedSymbol = NormalizeCurrencySymbol(NewCurrencySymbol);

        if (!IsValidCurrencyCode(normalizedCode))
        {
            HasErrorStatus = true;
            StatusMessage = "Currency code must be 3 to 10 uppercase letters (A-Z).";
            return;
        }

        if (!IsValidCurrencySymbol(normalizedSymbol))
        {
            HasErrorStatus = true;
            StatusMessage = "Currency symbol must not be empty and must be at most 10 characters.";
            return;
        }

        if (AvailableCurrencies.Any(currency => currency.CurrencyCode == normalizedCode))
        {
            HasErrorStatus = true;
            StatusMessage = $"Currency '{normalizedCode}' already exists.";
            return;
        }

        InsertCurrencyAlphabetically(new AccountingAvailableCurrencyData
        {
            OrganisationId = _organisationId,
            CurrencyCode = normalizedCode,
            CurrencySymbol = normalizedSymbol
        });

        if (!BaseCurrencyOptions.Any(option => option.CurrencyCode == SelectedBaseCurrency))
        {
            // Bootstrap empty-state: ensure the first added currency can be saved immediately.
            SelectedBaseCurrency = normalizedCode;
        }

        SelectedAvailableCurrencyCode = normalizedCode;
        NewCurrencyCode = string.Empty;
        NewCurrencySymbol = string.Empty;
        HasErrorStatus = false;
        StatusMessage = string.Empty;
        RefreshSaveState();
    }

    private bool CanAddCurrency()
    {
        return !IsSaving
               && !IsAwaitingPersistenceConfirmation
               && !string.IsNullOrWhiteSpace(NewCurrencyCode)
               && !string.IsNullOrWhiteSpace(NewCurrencySymbol);
    }

    [RelayCommand(CanExecute = nameof(CanRemoveSelectedCurrency))]
    private void RemoveSelectedCurrency()
    {
        if (!CanRemoveSelectedCurrency())
        {
            return;
        }

        if (AvailableCurrencies.Count <= 1)
        {
            HasErrorStatus = true;
            StatusMessage = "At least one available currency is required.";
            return;
        }

        var itemToRemove = AvailableCurrencies.FirstOrDefault(currency => currency.CurrencyCode == SelectedAvailableCurrencyCode);
        if (itemToRemove is null)
        {
            return;
        }

        AvailableCurrencies.Remove(itemToRemove);

        if (SelectedBaseCurrency == itemToRemove.CurrencyCode)
        {
            SelectedBaseCurrency = AvailableCurrencies.First().CurrencyCode;
        }

        SelectedAvailableCurrencyCode = AvailableCurrencies.FirstOrDefault()?.CurrencyCode ?? string.Empty;
        HasErrorStatus = false;
        StatusMessage = string.Empty;
        RefreshSaveState();
    }

    private bool CanRemoveSelectedCurrency()
    {
        return !IsSaving
               && !IsAwaitingPersistenceConfirmation
               && !string.IsNullOrWhiteSpace(SelectedAvailableCurrencyCode)
               && AvailableCurrencies.Any(currency => currency.CurrencyCode == SelectedAvailableCurrencyCode);
    }

    // Operation: resolves the current organisation id from the active user session.
    private static string ResolveOrganisationId(IUserSessionService userSessionService)
    {
        return userSessionService.CurrentUserSession?.Organisation?.Id
               ?? throw new InvalidOperationException("An active session with an organisation is required to manage accounting settings.");
    }

    // Operation: applies a received settings snapshot to the observable properties.
    private void ApplySettings(AccountingSettingsData? settings)
    {
        if (settings is null || settings.OrganisationId != _organisationId)
        {
            return;
        }

        var persistedCurrencies = BuildPersistedCurrencies(settings, _organisationId);
        var normalizedBaseCurrency = NormalizeCurrencyCode(settings.BaseCurrency);

        _persistedBaseCurrency = normalizedBaseCurrency;
        _persistedDateFormat = settings.DateFormat;
        _persistedDecimalPrecision = settings.DecimalPrecision;
        _persistedAvailableCurrencySnapshotKeys = BuildAvailableCurrencySnapshotKeys(persistedCurrencies);
        _hasPersistedSnapshot = true;

        AvailableCurrencies.Clear();
        foreach (var currency in persistedCurrencies)
        {
            AvailableCurrencies.Add(currency);
        }

        SelectedBaseCurrency = persistedCurrencies.Any(currency => currency.CurrencyCode == normalizedBaseCurrency)
            ? normalizedBaseCurrency
            : persistedCurrencies.First().CurrencyCode;
        SelectedDateFormat = settings.DateFormat;
        SelectedDecimalPrecision = settings.DecimalPrecision;
        SelectedAvailableCurrencyCode = persistedCurrencies.FirstOrDefault()?.CurrencyCode ?? string.Empty;

        RefreshSaveState();
    }

    // Operation: surfaces stream failures in the status banner.
    private void HandleStreamError(Exception ex)
    {
        _pendingSaveConfirmation?.TrySetException(ex);
        IsAwaitingPersistenceConfirmation = false;
        HasErrorStatus = true;
        StatusMessage = $"Error loading settings: {ex.Message}";
    }

    // Operation: maps the selected UI date format to a .NET format token for preview rendering.
    private static string ResolveDotNetDateFormat(string selectedDateFormat)
    {
        return selectedDateFormat switch
        {
            "DD/MM/YYYY" => "dd/MM/yyyy",
            "MM/DD/YYYY" => "MM/dd/yyyy",
            "YYYY-MM-DD" => "yyyy-MM-dd",
            _ => "dd/MM/yyyy"
        };
    }

    private static async Task WaitForSaveConfirmation(TaskCompletionSource pendingSaveConfirmation)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        using var timeoutRegistration = timeout.Token.Register(
            () => pendingSaveConfirmation.TrySetException(new TimeoutException("Timed out while waiting for settings to be persisted.")));

        await pendingSaveConfirmation.Task;
    }

    // Operation: extracts, normalizes, and defaults persisted currencies with symbols.
    private static List<AccountingAvailableCurrencyData> BuildPersistedCurrencies(AccountingSettingsData settings, string organisationId)
    {
        var normalized = (settings.AvailableCurrencies ?? [])
            .Select(currency => new AccountingAvailableCurrencyData
            {
                OrganisationId = organisationId,
                CurrencyCode = NormalizeCurrencyCode(currency.CurrencyCode),
                CurrencySymbol = NormalizeCurrencySymbol(currency.CurrencySymbol)
            })
            .Where(IsValidCurrency)
            .GroupBy(currency => currency.CurrencyCode)
            .Select(group => group.First())
            .OrderBy(currency => currency.CurrencyCode)
            .ToList();

        var normalizedBase = NormalizeCurrencyCode(settings.BaseCurrency);
        if (normalized.All(currency => currency.CurrencyCode != normalizedBase))
        {
            normalized.Add(new AccountingAvailableCurrencyData
            {
                OrganisationId = organisationId,
                CurrencyCode = normalizedBase,
                CurrencySymbol = normalizedBase
            });
        }

        if (normalized.Count == 0)
        {
            normalized.Add(new AccountingAvailableCurrencyData
            {
                OrganisationId = organisationId,
                CurrencyCode = "USD",
                CurrencySymbol = "$"
            });
        }

        return normalized
            .GroupBy(currency => currency.CurrencyCode)
            .Select(group => group.First())
            .OrderBy(currency => currency.CurrencyCode)
            .ToList();
    }

    private static IReadOnlyList<string> BuildAvailableCurrencySnapshotKeys(IEnumerable<AccountingAvailableCurrencyData> currencies)
    {
        return currencies
            .OrderBy(currency => currency.CurrencyCode)
            .Select(currency => $"{NormalizeCurrencyCode(currency.CurrencyCode)}|{NormalizeCurrencySymbol(currency.CurrencySymbol)}")
            .ToList();
    }

    private string ResolveBaseCurrencySymbol()
    {
        var selected = AvailableCurrencies.FirstOrDefault(currency => currency.CurrencyCode == SelectedBaseCurrency);
        return selected?.CurrencySymbol ?? SelectedBaseCurrency;
    }

    private static string NormalizeCurrencyCode(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static string NormalizeCurrencySymbol(string value)
    {
        return value.Trim();
    }

    private static bool IsValidCurrencyCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length is < 3 or > 10)
        {
            return false;
        }

        return value.All(char.IsLetter);
    }

    private static bool IsValidCurrencySymbol(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Length <= 10;
    }

    private static bool IsValidCurrency(AccountingAvailableCurrencyData currency)
    {
        return IsValidCurrencyCode(currency.CurrencyCode) && IsValidCurrencySymbol(currency.CurrencySymbol);
    }

    private void InsertCurrencyAlphabetically(AccountingAvailableCurrencyData currency)
    {
        var insertIndex = 0;
        while (insertIndex < AvailableCurrencies.Count
               && string.CompareOrdinal(AvailableCurrencies[insertIndex].CurrencyCode, currency.CurrencyCode) < 0)
        {
            insertIndex++;
        }

        AvailableCurrencies.Insert(insertIndex, currency);
    }

    partial void OnSelectedBaseCurrencyChanged(string value)
    {
        RefreshSaveState();
    }

    partial void OnSelectedDateFormatChanged(string value)
    {
        RefreshSaveState();
    }

    partial void OnSelectedDecimalPrecisionChanged(int value)
    {
        RefreshSaveState();
    }

    partial void OnStatusMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasStatusMessage));
    }

    private void RefreshSaveState()
    {
        OnPropertyChanged(nameof(BaseCurrencyOptions));
        OnPropertyChanged(nameof(IsSelectionValid));
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(FormattingPreview));
        SaveSettingsCommand.NotifyCanExecuteChanged();
        AddCurrencyCommand.NotifyCanExecuteChanged();
        RemoveSelectedCurrencyCommand.NotifyCanExecuteChanged();
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
    }
}

