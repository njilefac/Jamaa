namespace Jamaa.Desktop.Services.Interactions;

public interface IResultProvider<TResult>
{
    TResult Result { get; }
}