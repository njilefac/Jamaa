namespace Jamaa.Desktop.Services.Interactions;

public record InteractionContext<TInput, TOutput>(TInput Input)
{
    public TOutput Output { get; private set; } = default!;
    public bool IsHandled { get; private set; }

    public void SetOutput(TOutput output)
    {
        Output = output;
        IsHandled = true;
    }
}