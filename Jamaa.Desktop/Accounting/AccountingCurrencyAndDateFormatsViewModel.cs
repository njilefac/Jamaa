using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jamaa.Application.Finances;
using Jamaa.Application.Users.Services;
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

    public IReadOnlyList<string> BaseCurrencyOptions { get; } = ["USD", "KES", "EUR", "GBP", "ZAR", "NGN"];
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
    private bool _isSaving;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveSettingsCommand))]
    private bool _isAwaitingPersistenceConfirmation;

    [ObservableProperty]
    private bool _hasErrorStatus;

    public bool HasUnsavedChanges =>
        _hasPersistedSnapshot &&
        (SelectedBaseCurrency != _persistedBaseCurrency ||
         SelectedDateFormat != _persistedDateFormat ||
         SelectedDecimalPrecision != _persistedDecimalPrecision);

    public bool IsSelectionValid =>
        BaseCurrencyOptions.Any(option => option == SelectedBaseCurrency) &&
        DateFormatOptions.Any(option => option == SelectedDateFormat) &&
        DecimalPrecisionOptions.Any(option => option == SelectedDecimalPrecision);

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public string FormattingPreview
    {
        get
        {
            var exampleDate = new DateTime(2026, 4, 24).ToString(ResolveDotNetDateFormat(SelectedDateFormat), CultureInfo.InvariantCulture);
            var exampleAmount = 1234567.0m.ToString($"N{SelectedDecimalPrecision}", CultureInfo.InvariantCulture);
            return $"{SelectedBaseCurrency} {exampleAmount}   |   {exampleDate}";
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
            await _financeManagementFacade.UpdateAccountingSettings(
                _organisationId,
                SelectedBaseCurrency,
                SelectedDateFormat,
                SelectedDecimalPrecision);

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

    // Operation: resolves the current organisation id from the active user session.
    private static string ResolveOrganisationId(IUserSessionService userSessionService)
    {
        return userSessionService.CurrentUserSession?.Organisation?.Id
               ?? throw new InvalidOperationException("An active session with an organisation is required to manage accounting settings.");
    }

    // Operation: applies a received settings snapshot to the observable properties.
    private void ApplySettings(Jamaa.Data.Models.Finances.AccountingSettingsData? settings)
    {
        if (settings is null || settings.OrganisationId != _organisationId)
        {
            return;
        }

        _persistedBaseCurrency = settings.BaseCurrency;
        _persistedDateFormat = settings.DateFormat;
        _persistedDecimalPrecision = settings.DecimalPrecision;
        _hasPersistedSnapshot = true;

        SelectedBaseCurrency = settings.BaseCurrency;
        SelectedDateFormat = settings.DateFormat;
        SelectedDecimalPrecision = settings.DecimalPrecision;

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
        OnPropertyChanged(nameof(IsSelectionValid));
        OnPropertyChanged(nameof(HasUnsavedChanges));
        SaveSettingsCommand.NotifyCanExecuteChanged();
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
    }
}

