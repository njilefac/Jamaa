using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Libota.Desktop.Navigation;

public class NavigationScope : INavigationScope
{
    private readonly Stack<object> _stack = new();
    private readonly Dictionary<string, object> _cache = new();
    private readonly BehaviorSubject<object> _state = new (null!);
    private readonly IRouteResolver _routeResolver;

    public NavigationScope(IRouteResolver routeResolver)
    {
        _routeResolver = routeResolver;
        Navigated = _state;
    }

    public IObservable<object> Navigated {get;} 

    public bool CanGoBack => _stack.Count > 1;
    public Task NavigateToAsync(string route, object? parameter = null)
    {
        if (!_cache.TryGetValue(route, out var vm))
        {
            vm = _routeResolver.Resolve(route, parameter);
            _cache[route] = vm;
        }

        _stack.Push(vm);
        _state.OnNext(vm);
        return Task.CompletedTask;
    }

    public Task GoBack()
    {
        if (!CanGoBack)
        {
            return Task.FromResult(_stack.Peek());
        }
        _stack.Pop();
        _state.OnNext(_stack.Peek());
        return Task.CompletedTask;
    }
}