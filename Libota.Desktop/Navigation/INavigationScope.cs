using System;
using System.Threading.Tasks;

namespace Libota.Desktop.Navigation;

public interface INavigationScope
{
    bool CanGoBack { get; }
    Task NavigateToAsync(string route, object? parameter = null);
    Task GoBack();
    
    IObservable<object?> Navigated { get; }
}