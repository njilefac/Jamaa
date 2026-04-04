using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Threading;
using Jamaa.Desktop.Services;

namespace Jamaa.Desktop.Shared;

public partial class SplashViewModel : ObservableObject, IDisposable
{
    [ObservableProperty]
    private string _status = "Initializing...";

    [ObservableProperty]
    private double _progress;

    private readonly IDisposable _statusSubscription;
    private readonly IDisposable _progressSubscription;

    public SplashViewModel()
    {
        _statusSubscription = InitializationService.Status
            .Subscribe(status => Dispatcher.UIThread.Post(() => Status = status));
        _progressSubscription = InitializationService.Progress
            .Subscribe(progress => Dispatcher.UIThread.Post(() => Progress = progress));
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _statusSubscription.Dispose();
        _progressSubscription.Dispose();
    }
}
