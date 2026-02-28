namespace Libota.Desktop.Services.Interactions;

public interface IResultProvider<TResult>
{
    TResult Result { get; }
}