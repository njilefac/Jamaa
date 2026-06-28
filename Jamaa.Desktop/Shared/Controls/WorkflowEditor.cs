using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NodeEditor.Controls;

namespace Jamaa.Desktop.Shared.Controls;

/// <summary>
/// A custom control for editing workflows, based on NodeEditorAvalonia's Editor.
/// </summary>
public class WorkflowEditor : UserControl
{
    public WorkflowEditor()
    {
        Content = new Editor();
    }
}
