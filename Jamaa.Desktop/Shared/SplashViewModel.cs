using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Threading;
using Jamaa.Desktop.Services;

namespace Jamaa.Desktop.Shared;

public partial class SplashViewModel : ObservableObject, IDisposable
{
    [ObservableProperty]
    private string _status = "Initializing...";

    private readonly IDisposable _statusSubscription;

    public SplashViewModel()
    {
        _statusSubscription = InitializationService.Status
            .Subscribe(status => Dispatcher.UIThread.Post(() => Status = status));
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _statusSubscription.Dispose();
    }
}
