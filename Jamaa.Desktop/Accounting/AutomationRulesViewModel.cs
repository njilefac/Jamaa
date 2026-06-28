using System;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Shared;
using NodifyM.Avalonia.ViewModelBase;

namespace Jamaa.Desktop.Accounting;

public partial class AutomationRulesViewModel : NodifyEditorViewModelBase, IApplicationModule, IRouteableViewModel
{
    [ObservableProperty] private string? _elsaServerErrorMessage;
    [ObservableProperty] private bool _hasElsaServerError;
    [ObservableProperty] private bool _showStatusMessage;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public AutomationRulesViewModel()
    {
        var start = new WorkflowNodeViewModel("Start", "Trigger", new Point(120, 180), 0, 1);
        var approval = new WorkflowNodeViewModel("Approval", "Validate request", new Point(420, 120), 1, 2);
        var complete = new WorkflowNodeViewModel("Complete", "Mark done", new Point(760, 80), 1, 0);
        var reject = new WorkflowNodeViewModel("Reject", "Notify requester", new Point(760, 280), 1, 0);

        Nodes.Add(start);
        Nodes.Add(approval);
        Nodes.Add(complete);
        Nodes.Add(reject);

        Connections.Add(new ConnectionViewModelBase(this, start.OutputConnector(0), approval.InputConnector(0), "Next"));
        Connections.Add(new ConnectionViewModelBase(this, approval.OutputConnector(0), complete.InputConnector(0), "Approved"));
        Connections.Add(new ConnectionViewModelBase(this, approval.OutputConnector(1), reject.InputConnector(0), "Rejected"));
    }

    public Guid Id => Guid.Parse("29315df6-29e2-40f9-81ac-8b3431df7b1a");
    public string Title => "Automation Rules";
    public object? HeaderContent => null;

    private sealed class WorkflowNodeViewModel : NodeViewModelBase
    {
        public WorkflowNodeViewModel(string title, string footer, Point location, int inputCount, int outputCount)
        {
            Title = title;
            Footer = footer;
            Location = location;

            for (var index = 0; index < inputCount; index++)
            {
                Input.Add(new ConnectorViewModelBase
                {
                    Title = inputCount == 1 ? "In" : $"In {index + 1}",
                    Flow = ConnectorViewModelBase.ConnectorFlow.Input
                });
            }

            for (var index = 0; index < outputCount; index++)
            {
                Output.Add(new ConnectorViewModelBase
                {
                    Title = outputCount == 1 ? "Out" : $"Out {index + 1}",
                    Flow = ConnectorViewModelBase.ConnectorFlow.Output
                });
            }
        }

        public ConnectorViewModelBase InputConnector(int index) => (ConnectorViewModelBase)Input[index];

        public ConnectorViewModelBase OutputConnector(int index) => (ConnectorViewModelBase)Output[index];
    }
}
