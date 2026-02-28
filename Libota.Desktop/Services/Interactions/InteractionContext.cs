
namespace Libota.Desktop.Services.Interactions;

public record InteractionContext<TInput, TOutput>(TInput Input)
{
    public void SetOutput(TOutput output)
    {
        Output = output;
    }

    public TOutput Output { get; private set; }
}
