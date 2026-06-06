using Avalonia.Controls;
using System;

namespace Jamaa.Desktop.Accounting.Wizard;

public partial class AccountingSetupWizardPage : UserControl
{
    public AccountingSetupWizardPage()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _ = sender;

        if (DataContext is AccountingSetupWizardViewModel viewModel)
            _ = viewModel.InitializeAsync();
    }
}
