using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Libota.Desktop.Navigation;

public partial class NavigationStore: ObservableObject
{
    private readonly Stack<ObservableObject?> _state = new();
    public bool CanGoBack => _state.Count > 1;

    public void Push(ObservableObject viewModel)
    {
        if(_state.Count == 0)
        {
            _state.Push(viewModel);
            CurrentViewModel = viewModel;
            return;
        }
        
        var current = _state.Peek();
        if(viewModel.GetType() == current?.GetType()) {return;}
        
        _state.Push(viewModel);
        CurrentViewModel = viewModel;
    }
    
    public void Pop()
    {
        if (_state.Count <= 1) {return;}
        CurrentViewModel = _state.Pop();
    }
    
    [ObservableProperty]
    private ObservableObject? _currentViewModel;
}