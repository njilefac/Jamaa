namespace Libota.Desktop.Infrastructure;

public record InteractionContext<TInput, TOutput>(TInput Input)
{
    public void SetOutput(TOutput output)
    {
        Output = output;
    }

    public TOutput? Output { get; set; }
}
