namespace Libota.Desktop.Infrastructure.Interactions;

public interface IResultProvider<TResult>
{
    TResult Result { get; }
}