using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Libota.Desktop.Navigation;

public class NavigationService(NavigationStore store, IServiceProvider provider) : INavigationService
{
    public Task NavigateTo<TViewModel>() where TViewModel : ObservableObject
    {
        var vm = provider.GetRequiredService<TViewModel>();
        store.CurrentViewModel = vm;
        return Task.CompletedTask;
    }

    public Task GoBack()
    {
        throw new NotImplementedException();
    }

    public Task NavigateTo(ObservableObject viewModel)
    {
        store.CurrentViewModel = viewModel;
        return Task.CompletedTask;
    }

    public void NavigateTo(Type? viewModelType)
    {
        var viewModel =
            provider.GetRequiredService(viewModelType ?? throw new ArgumentNullException(nameof(viewModelType)));
        NavigateTo((ObservableObject)viewModel);
    }
}