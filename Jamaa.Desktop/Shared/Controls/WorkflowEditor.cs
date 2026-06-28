using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using NodifyM.Avalonia.Controls;
using NodifyM.Avalonia.ViewModelBase;

namespace Jamaa.Desktop.Shared.Controls;

public sealed class WorkflowEditor : UserControl
{
    private readonly WorkflowGraphViewModel _graph = new();

    public WorkflowEditor()
    {
        var editor = new NodifyEditor
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ItemsSource = _graph.Nodes,
            Connections = _graph.Connections,
            PendingConnection = _graph.PendingConnection,
            DisconnectConnectorCommand = _graph.DisconnectConnectorCommand
        };

        Content = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(20),
            Child = editor
        };
    }

    private sealed class WorkflowGraphViewModel : NodifyEditorViewModelBase
    {
        public WorkflowGraphViewModel()
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
    }

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
