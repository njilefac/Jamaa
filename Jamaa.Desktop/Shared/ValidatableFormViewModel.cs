using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Jamaa.Desktop.Shared;

public abstract partial class ValidatableFormViewModel : ObservableValidator, INotifyDataErrorInfo
{
    private readonly HashSet<string> _touchedProperties = new();

    [ObservableProperty] private bool _showAllErrors;

    public ValidatableFormViewModel()
    {
        base.ErrorsChanged += (s, e) =>
        {
            if (string.IsNullOrEmpty(e.PropertyName) ||
                ShowAllErrors ||
                _touchedProperties.Contains(e.PropertyName))
                ErrorsChanged?.Invoke(this, e);
        };
    }

    // We shadow or implement the event to ensure we can trigger it
    public new event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    // Explicit implementation to provide the "Dirty/Touched" filtering logic
    IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) return Enumerable.Empty<object>();

        // Logic: Only show errors if 'ShowAllErrors' is true OR the user touched it.
        if (ShowAllErrors || _touchedProperties.Contains(propertyName)) return base.GetErrors(propertyName);

        return Enumerable.Empty<object>();
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (string.IsNullOrEmpty(e.PropertyName) ||
            e.PropertyName == nameof(HasErrors) ||
            e.PropertyName == nameof(ShowAllErrors))
            return;

        if (_touchedProperties.Add(e.PropertyName))
            // Manually fire the event that Avalonia is listening to.
            // This tells Avalonia: "Re-run GetErrors for this property."
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(e.PropertyName));
    }

    [RelayCommand]
    public virtual void ResetValidationState()
    {
        ShowAllErrors = false;
        _touchedProperties.Clear();
        ClearErrors();

        // Refresh all bindings and clear red borders
        OnPropertyChanged(string.Empty);

        // Notify the UI that errors for ALL properties have potentially changed
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(null));
    }

    protected bool ValidateAndShow()
    {
        ValidateAllProperties();
        ShowAllErrors = true;

        // Force UI refresh
        OnPropertyChanged(string.Empty);
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(null));

        return !HasErrors;
    }
}