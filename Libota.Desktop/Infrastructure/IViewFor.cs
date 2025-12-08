using CommunityToolkit.Mvvm.ComponentModel;

namespace Libota.Desktop.Infrastructure;

public interface IViewFor<TViewModel> where TViewModel : ObservableObject
{
    TViewModel? DataContext { get; set; }
}