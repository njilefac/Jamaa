using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Libota.Desktop.Navigation;

public interface INavigationService
{
    Task NavigateTo<TViewModel>() where TViewModel : ObservableObject;
    Task GoBack();
    Task NavigateTo(ObservableObject viewModel);
    void NavigateTo(Type? viewModelType);
    
    IObservable<ObservableObject> ViewChanged { get;  }
}