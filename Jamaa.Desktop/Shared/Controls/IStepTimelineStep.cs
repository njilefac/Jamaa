namespace Jamaa.Desktop.Shared.Controls;

public interface IStepTimelineStep
{
    string Title { get; }
    string Description { get; }
    int StepNumber { get; }
    bool IsCompleted { get; }
    bool IsEnabled { get; }
    bool HasConnector { get; }
}

