
namespace Jamaa.Desktop.Services.Interactions;

public record InteractionContext<TInput, TOutput>(TInput Input)
{
    public void SetOutput(TOutput output)
    {
        Output = output;
        IsHandled = true;
    }

    public TOutput Output { get; private set; }
    public bool IsHandled { get; private set; }
}
