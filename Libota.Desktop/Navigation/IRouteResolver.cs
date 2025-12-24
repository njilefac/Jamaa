namespace Libota.Desktop.Navigation;

public interface IRouteResolver
{
    object Resolve(string route, object? parameter = null);
}