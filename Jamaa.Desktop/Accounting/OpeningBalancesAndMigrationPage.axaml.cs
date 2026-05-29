using System;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;

namespace Jamaa.Desktop.Accounting;

public partial class OpeningBalancesAndMigrationPage : UserControl
{
    public OpeningBalancesAndMigrationPage()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        ConfigureTreeGrid();
    }

    private void OnDataContextChanged(object? sender, EventArgs e) => ConfigureTreeGrid();

    // Integration: owns TreeDataGrid source and column definitions at the view layer.
    private void ConfigureTreeGrid()
    {
        if (DataContext is not OpeningBalancesAndMigrationViewModel viewModel) return;

        if (Resources["BalanceCellTemplate"] is not IDataTemplate balanceCellTemplate ||
            Resources["SaveCellTemplate"] is not IDataTemplate saveCellTemplate)
            return;

        var source = new HierarchicalTreeDataGridSource<OpeningBalanceItemViewModel>(viewModel.Accounts)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<OpeningBalanceItemViewModel>(
                    new TextColumn<OpeningBalanceItemViewModel, string>(
                        "Code",
                        item => item.Code,
                        options: new TextColumnOptions<OpeningBalanceItemViewModel>
                        {
                            CanUserSortColumn = false
                        }),
                    item => item.SubAccounts),

                new TextColumn<OpeningBalanceItemViewModel, string>(
                    "Account Name",
                    item => item.Name,
                    options: new TextColumnOptions<OpeningBalanceItemViewModel>
                    {
                        CanUserSortColumn = false
                    }),

                new TextColumn<OpeningBalanceItemViewModel, string>(
                    "Type",
                    item => item.TypeDisplay,
                    options: new TextColumnOptions<OpeningBalanceItemViewModel>
                    {
                        CanUserSortColumn = false
                    }),

                new TemplateColumn<OpeningBalanceItemViewModel>("Opening Balance", balanceCellTemplate),

                new TemplateColumn<OpeningBalanceItemViewModel>(string.Empty, saveCellTemplate)
            }
        };

        BalancesTreeDataGrid.Source = source;
    }
}