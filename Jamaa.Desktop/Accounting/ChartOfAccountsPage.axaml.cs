using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;

namespace Jamaa.Desktop.Accounting;

public partial class ChartOfAccountsPage : UserControl
{
    public ChartOfAccountsPage()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => ConfigureActionColumns();
        ConfigureActionColumns();
    }

    // Integration: owns UI column composition and templates at the view layer.
    private void ConfigureActionColumns()
    {
        if (DataContext is not ChartOfAccountsViewModel viewModel || viewModel.Source is null) return;

        if (Resources["AccountEditCellTemplate"] is not IDataTemplate editCellTemplate ||
            Resources["AccountActiveStateCellTemplate"] is not IDataTemplate toggleActiveCellTemplate ||
            Resources["AccountLedgerCellTemplate"] is not IDataTemplate ledgerCellTemplate)
            return;

        var columns = viewModel.Source.Columns;
        while (columns.Count > 4) columns.RemoveAt(columns.Count - 1);

        columns.Add(new TemplateColumn<AccountItemViewModel>(string.Empty, editCellTemplate));
        columns.Add(new TemplateColumn<AccountItemViewModel>(string.Empty, toggleActiveCellTemplate));
        columns.Add(new TemplateColumn<AccountItemViewModel>(string.Empty, ledgerCellTemplate));
    }
}