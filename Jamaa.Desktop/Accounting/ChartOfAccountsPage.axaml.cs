using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Templates;

namespace Jamaa.Desktop.Accounting;

public partial class ChartOfAccountsPage : UserControl
{
    private TreeDataGridRowSelectionModel<AccountItemViewModel>? _selection;
    private ChartOfAccountsViewModel? _viewModel;

    public ChartOfAccountsPage()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        ConfigureTreeGrid();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        DetachViewModelHandlers();
        ConfigureTreeGrid();
    }

    // Integration: owns TreeDataGrid source, columns, and selection bridging at the view layer.
    private void ConfigureTreeGrid()
    {
        if (DataContext is not ChartOfAccountsViewModel viewModel) return;

        if (Resources["AccountEditCellTemplate"] is not IDataTemplate editCellTemplate ||
            Resources["OpeningBalanceCellTemplate"] is not IDataTemplate openingBalanceCellTemplate ||
            Resources["SaveOpeningBalanceCellTemplate"] is not IDataTemplate saveOpeningBalanceCellTemplate ||
            Resources["AccountActiveStateCellTemplate"] is not IDataTemplate toggleActiveCellTemplate ||
            Resources["AccountLedgerCellTemplate"] is not IDataTemplate ledgerCellTemplate)
            return;

        _viewModel = viewModel;

        var source = new HierarchicalTreeDataGridSource<AccountItemViewModel>(viewModel.Accounts)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<AccountItemViewModel>(
                    new TextColumn<AccountItemViewModel, string>("Code", account => account.Code,
                        options: new TextColumnOptions<AccountItemViewModel> { CanUserSortColumn = true }),
                    account => account.SubAccounts),
                new TextColumn<AccountItemViewModel, string>("Name", account => account.Name,
                    options: new TextColumnOptions<AccountItemViewModel> { CanUserSortColumn = true }),
                new TextColumn<AccountItemViewModel, string>("Description", account => account.Description,
                    options: new TextColumnOptions<AccountItemViewModel> { CanUserSortColumn = true }),
                new TextColumn<AccountItemViewModel, string>("Type", account => account.TypeDisplay,
                    options: new TextColumnOptions<AccountItemViewModel> { CanUserSortColumn = true }),
                new TemplateColumn<AccountItemViewModel>("Opening Balance", openingBalanceCellTemplate),
                new TemplateColumn<AccountItemViewModel>(string.Empty, saveOpeningBalanceCellTemplate),
                new TemplateColumn<AccountItemViewModel>(string.Empty, editCellTemplate),
                new TemplateColumn<AccountItemViewModel>(string.Empty, toggleActiveCellTemplate),
                new TemplateColumn<AccountItemViewModel>(string.Empty, ledgerCellTemplate)
            }
        };

        _selection = new TreeDataGridRowSelectionModel<AccountItemViewModel>(source)
        {
            SingleSelect = true
        };

        _selection.SelectionChanged += OnSelectionChanged;
        source.Selection = _selection;
        AccountsTreeDataGrid.Source = source;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnSelectionChanged(object? sender, TreeSelectionModelSelectionChangedEventArgs<AccountItemViewModel> e)
    {
        _ = sender;
        _ = e;

        if (_viewModel is null || _selection is null) return;
        _viewModel.SelectedAccount = _selection.SelectedItem;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = sender;

        if (e.PropertyName != nameof(ChartOfAccountsViewModel.SelectedAccount)) return;
        if (_viewModel?.SelectedAccount is not null) return;

        _selection?.Clear();
    }

    private void DetachViewModelHandlers()
    {
        if (_selection is not null)
            _selection.SelectionChanged -= OnSelectionChanged;

        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _selection = null;
        _viewModel = null;
    }
}
