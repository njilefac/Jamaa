using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Libota.Desktop.Navigation;

public class NavigationService(NavigationStore store, IServiceProvider provider) : INavigationService
{
    private readonly Subject<ObservableObject> _viewChanges = new();

    public Task NavigateTo<TViewModel>() where TViewModel : ObservableObject
    {
        var vm = provider.GetRequiredService<TViewModel>();
        _viewChanges.OnNext(vm);
        return NavigateTo(vm);
    }

    public Task GoBack()
    {
        if (store.CanGoBack is false)
        {
            return Task.CompletedTask;
        }
        
        store.Pop();
        if (store.CurrentViewModel is { } vm)
        {
            _viewChanges.OnNext(vm);
        }
        return Task.CompletedTask;
    }

    public Task NavigateTo(ObservableObject viewModel)
    {
        store.Push(viewModel);
        _viewChanges.OnNext(viewModel);
        return Task.CompletedTask;
    }

    public void NavigateTo(Type? viewModelType)
    {
        var viewModel =
            provider.GetRequiredService(viewModelType ?? throw new ArgumentNullException(nameof(viewModelType)));
        NavigateTo((ObservableObject)viewModel);
        _viewChanges.OnNext(viewModel as ObservableObject ?? throw new InvalidOperationException());
    }

    public IObservable<ObservableObject> ViewChanged => _viewChanges;
}