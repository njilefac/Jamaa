using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Domain.Users;
using Jamaa.Application.Users.Services;
using Jamaa.Desktop.Security.Events;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Dashboard;

public partial class DashboardViewModel : ObservableObject, 
    IApplicationModule,
    IRecipient<UserLoggedOut>,
    IRecipient<UserAuthenticated>
{
    private readonly IUserSessionService _userSessionService;
    private readonly IUserRepository _userRepository;

    private static int MaxColumns => 3;
    private static int MaxRows => 2;

    public ObservableCollection<WidgetViewModelBase> ActiveWidgets { get; set; } = [];
    public ObservableCollection<WidgetViewModelBase> AvailableWidgets { get; set; } = [];

    public DashboardViewModel(IUserSessionService userSessionService, IUserRepository userRepository)
    {
        _userSessionService = userSessionService;
        _userRepository = userRepository;
        
        // Register first so we can receive messages
        WeakReferenceMessenger.Default.RegisterAll(this);

        UpdateAvailableWidgets();
    }

    public void Receive(UserLoggedOut message)
    {
        _ = SaveLayout();
    }

    public void Receive(UserAuthenticated message)
    {
        _ = LoadLayout(message.Session);
    }

    private bool CanAddWidget(object? parameter)
    {
        if (parameter is object[] values && values.Length == 2 && values[0] is EmptyCellViewModel cell)
        {
            return cell.HasCompatibleWidgets;
        }
        return AvailableWidgets.Any();
    }

    private void UpdateAvailableWidgets()
    {
        // Define all possible non-empty widget types
        var allPossibleWidgets = new List<WidgetViewModelBase>
        {
            new ReportingAndAnalyticsWidgetViewModel(),
            new BookkeepingWidgetViewModel(),
            new RecentActivityFeedWidgetViewModel(),
            new CalendarScheduleWidgetViewModel(),
            new AlertsAndNotificationsWidgetViewModel(),
            new QuickActionsWidgetViewModel(),
            new MembershipStatsWidgetViewModel()
        };

        // Get types of currently active widgets
        var activeTypes = ActiveWidgets
            .Where(w => w is not EmptyCellViewModel)
            .Select(w => w.GetType())
            .ToHashSet();

        // Filter and refill AvailableWidgets with types not in activeTypes
        AvailableWidgets.Clear();
        foreach (var widget in allPossibleWidgets)
        {
            if (!activeTypes.Contains(widget.GetType()))
            {
                widget.ParentViewModel = this;
                AvailableWidgets.Add(widget);
            }
        }

        foreach (var widget in ActiveWidgets)
        {
            widget.NotifyCompatibilityChanged();
        }
        
        AddWidgetCommand.NotifyCanExecuteChanged();
        ReplaceWidgetCommand.NotifyCanExecuteChanged();
    }

    public IEnumerable<WidgetViewModelBase> GetCompatibleWidgets(BoxSize size)
    {
        return AvailableWidgets.Where(w => w.AllowedBoxSize == size);
    }

    [RelayCommand(CanExecute = nameof(CanAddWidget))]
    private void AddWidget(object? parameter)
    {
        if (parameter is not object[] values || values.Length != 2) return;
        if (values[0] is not EmptyCellViewModel cell || values[1] is not WidgetViewModelBase widgetToAdd) return;

        widgetToAdd.Row = cell.Row;
        widgetToAdd.Column = cell.Column;
        widgetToAdd.ParentViewModel = this;
        widgetToAdd.RemoveCommand = new RelayCommand<WidgetViewModelBase>(RemoveWidget);

        ActiveWidgets.Remove(cell);
        ActiveWidgets.Add(widgetToAdd);
        UpdateAvailableWidgets();
        _ = SaveLayout();
    }

    [RelayCommand(CanExecute = nameof(CanReplaceWidget))]
    private void ReplaceWidget(object? parameter)
    {
        if (parameter is not object[] values || values.Length != 2) return;
        if (values[0] is not WidgetViewModelBase oldWidget || values[1] is not WidgetViewModelBase newWidget) return;

        newWidget.Row = oldWidget.Row;
        newWidget.Column = oldWidget.Column;
        newWidget.ParentViewModel = this;
        newWidget.RemoveCommand = new RelayCommand<WidgetViewModelBase>(RemoveWidget);

        ActiveWidgets.Remove(oldWidget);
        ActiveWidgets.Add(newWidget);
        UpdateAvailableWidgets();
        _ = SaveLayout();
    }

    private bool CanReplaceWidget(object? parameter)
    {
        if (parameter is WidgetViewModelBase widget)
        {
            return widget.HasCompatibleWidgets;
        }

        if (parameter is object[] { Length: 2 } values && values[0] is WidgetViewModelBase oldWidget)
        {
            return oldWidget.HasCompatibleWidgets;
        }

        return false;
    }

    private void RemoveWidget(WidgetViewModelBase? widget)
    {
        if (widget == null || !widget.IsRemovable) return;
        
        // Replace with an empty cell of the same size
        var boxSize = (widget.Column == 1) ? BoxSize.Wide : BoxSize.Small;
        var emptyCell = new EmptyCellViewModel(widget.Row, widget.Column, boxSize)
        {
            ParentViewModel = this
        };

        ActiveWidgets.Remove(widget);
        ActiveWidgets.Add(emptyCell);
        UpdateAvailableWidgets();
        _ = SaveLayout();
    }

    public async Task SaveLayout()
    {
        var session = _userSessionService.CurrentUserSession;
        if (session?.UserId == null) return;

        var options = new JsonSerializerOptions { WriteIndented = true };
        // Ignore empty widgets during serialization
        var widgetsToSave = ActiveWidgets.Where(w => w is not EmptyCellViewModel).ToList();
        string jsonString = JsonSerializer.Serialize(widgetsToSave, options);

        var user = await _userRepository.GetById(session.UserId.Value);
        if (user != null)
        {
            user.DashboardLayout = jsonString;
            await _userRepository.Update(user);
        }
    }

    internal async Task LoadLayout(Jamaa.Application.Users.UserSession? session = null)
    {
        session ??= _userSessionService.CurrentUserSession;
        if (session is { IsAuthenticated: true, UserId: not null })
        {
            try
            {
                var user = await _userRepository.GetById(session.UserId.Value);
                var jsonString = user?.DashboardLayout;

                if (!string.IsNullOrEmpty(jsonString))
                {
                    var loadedWidgets = JsonSerializer.Deserialize<ObservableCollection<WidgetViewModelBase>>(jsonString);

                    if (loadedWidgets != null && loadedWidgets.Any())
                    {
                        LoadDefaultGrid();
                        foreach (var widget in loadedWidgets)
                        {
                            widget.ParentViewModel = this;
                            widget.RemoveCommand = new RelayCommand<WidgetViewModelBase>(RemoveWidget);

                            // Find the corresponding widget to replace
                            var existingWidgetAtPosition = ActiveWidgets.FirstOrDefault(w => 
                                w.Row == widget.Row && 
                                w.Column == widget.Column);

                            // Find if this widget type already exists in another position from the default grid
                            var existingSameTypeWidget = ActiveWidgets.FirstOrDefault(w => 
                                w.GetType() == widget.GetType() && 
                                (w.Row != widget.Row || w.Column != widget.Column));

                            if (existingSameTypeWidget != null)
                            {
                                var boxSize = (existingSameTypeWidget.Column == 1) ? BoxSize.Wide : BoxSize.Small;
                                var emptyCell = new EmptyCellViewModel(existingSameTypeWidget.Row, existingSameTypeWidget.Column, boxSize)
                                {
                                    ParentViewModel = this
                                };
                                ActiveWidgets.Remove(existingSameTypeWidget);
                                ActiveWidgets.Add(emptyCell);
                            }

                            if (existingWidgetAtPosition != null)
                            {
                                ActiveWidgets.Remove(existingWidgetAtPosition);
                            }
                            ActiveWidgets.Add(widget);
                        }
                        UpdateAvailableWidgets();
                        return;
                    }
                }
            }
            catch { /* Corrupt JSON or DB error, fall through to default grid */ }
        }
        
        LoadDefaultGrid();
        UpdateAvailableWidgets();
    }

    private void LoadDefaultGrid()
    {
        ActiveWidgets.Clear();
        for (var r = 0; r < MaxRows; r++)
        {
            for (var c = 0; c < MaxColumns; c++)
            {
                var boxSize = (c == 1) ? BoxSize.Wide : BoxSize.Small;
                WidgetViewModelBase widget = (r, c) switch
                {
                    (0, 0) => new MembershipStatsWidgetViewModel(),
                    (1, 0) => new RecentActivityFeedWidgetViewModel(),
                    (0, 1) => new BookkeepingWidgetViewModel(),
                    (1, 1) => new CalendarScheduleWidgetViewModel(),
                    (0, 2) => new AlertsAndNotificationsWidgetViewModel(),
                    (1, 2) => new QuickActionsWidgetViewModel(),
                    _ => new EmptyCellViewModel(r, c, boxSize)
                };

                widget.Row = r;
                widget.Column = c;
                widget.ParentViewModel = this;
                if (widget is not EmptyCellViewModel)
                {
                    widget.RemoveCommand = new RelayCommand<WidgetViewModelBase>(RemoveWidget);
                }

                ActiveWidgets.Add(widget);
            }
        }
    }

    [RelayCommand]
    private async Task ResetLayout()
    {
        LoadDefaultGrid();
        UpdateAvailableWidgets();
        await SaveLayout();
    }

    public Guid Id => Guid.Parse("30081491-B585-464D-AED1-BA3782D0E939");
    public string Title => "Dashboard";
    public object? HeaderContent => null;
}