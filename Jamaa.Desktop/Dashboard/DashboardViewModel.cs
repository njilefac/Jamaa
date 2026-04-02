using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Dashboard;

public partial class DashboardViewModel : ObservableObject, IApplicationModule
{
    private const string LayoutFilePath = "jamaa_dashboard_layout.json";

    public int MaxColumns { get; } = 3;
    public int MaxRows { get; } = 3;

    public ObservableCollection<WidgetViewModelBase> ActiveWidgets { get; set; } = [];
    public ObservableCollection<WidgetViewModelBase> AvailableWidgets { get; set; } = [];

    public DashboardViewModel()
    {
        LoadLayout();
        
        // Populate available widgets menu
        AvailableWidgets.Add(new AnalyticsWidgetViewModel());
        AvailableWidgets.Add(new BookkeepingWidgetViewModel());
    }

    [RelayCommand]
    private void AddWidget(WidgetViewModelBase widget)
    {
        // Find the first empty cell to replace
        var emptyCell = ActiveWidgets.FirstOrDefault(w => w is EmptyCellViewModel);
        if (emptyCell != null)
        {
            widget.Row = emptyCell.Row;
            widget.Column = emptyCell.Column;
            widget.ParentViewModel = this;
            widget.RemoveCommand = new RelayCommand<WidgetViewModelBase>(RemoveWidget);

            ActiveWidgets.Remove(emptyCell);
            ActiveWidgets.Add(widget);
            AvailableWidgets.Remove(widget);
            SaveLayout();
        }
    }

    private void RemoveWidget(WidgetViewModelBase? widget)
    {
        if (widget == null) return;
        
        // Replace with an empty cell
        var emptyCell = new EmptyCellViewModel
        {
            Row = widget.Row,
            Column = widget.Column,
            ParentViewModel = this
        };

        ActiveWidgets.Remove(widget);
        AvailableWidgets.Add(widget);
        ActiveWidgets.Add(emptyCell);
        SaveLayout();
    }

    public void SaveLayout()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(ActiveWidgets, options);
        File.WriteAllText(LayoutFilePath, jsonString);
    }

    private void LoadLayout()
    {
        if (File.Exists(LayoutFilePath))
        {
            try
            {
                string jsonString = File.ReadAllText(LayoutFilePath);
                var loadedWidgets = JsonSerializer.Deserialize<ObservableCollection<WidgetViewModelBase>>(jsonString);

                if (loadedWidgets != null && loadedWidgets.Any())
                {
                    ActiveWidgets.Clear();
                    foreach (var widget in loadedWidgets)
                    {
                        widget.ParentViewModel = this;
                        widget.RemoveCommand = new RelayCommand<WidgetViewModelBase>(RemoveWidget);
                        ActiveWidgets.Add(widget);
                    }
                    return;
                }
            }
            catch { /* Corrupt JSON, fall through to default */ }
        }

        LoadDefaultGrid();
    }

    private void LoadDefaultGrid()
    {
        ActiveWidgets.Clear();
        for (var r = 0; r < MaxRows; r++)
        {
            for (var c = 0; c < MaxColumns; c++)
            {
                ActiveWidgets.Add(new EmptyCellViewModel
                {
                    Row = r, Column = c, ParentViewModel = this
                });
            }
        }
    }

    public Guid Id => Guid.Parse("30081491-B585-464D-AED1-BA3782D0E939");
    public string Title => "Dashboard";
    public object? HeaderContent => null;
}