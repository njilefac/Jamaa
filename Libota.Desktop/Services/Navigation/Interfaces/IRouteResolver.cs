namespace Libota.Desktop.Services.Navigation.Interfaces;

public interface IRouteResolver
{
    object Resolve(string route, object? parameter = null);
}