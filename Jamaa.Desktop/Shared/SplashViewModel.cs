using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Jamaa.Desktop.Services;

namespace Jamaa.Desktop.Shared;

public partial class SplashViewModel : ObservableObject, IDisposable
{
    private readonly IDisposable _progressSubscription;

    private readonly IDisposable _statusSubscription;

    [ObservableProperty] private double _progress;

    [ObservableProperty] private string _status = "Initializing...";

    [ObservableProperty] private string _version;

    public SplashViewModel()
    {
        _version = VersionService.GetVersion();
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