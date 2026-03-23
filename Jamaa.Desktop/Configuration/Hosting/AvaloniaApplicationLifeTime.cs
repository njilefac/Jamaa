using System;
using System.Threading;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Hosting;

namespace Jamaa.Desktop.Configuration.Hosting;

public class AvaloniaApplicationLifeTime : IHostApplicationLifetime, IDisposable
{
    private readonly IControlledApplicationLifetime _avaloniaLifeTime;
    private readonly CancellationTokenSource _startedSource = new();
    private readonly CancellationTokenSource _stoppingSource = new();
    private readonly CancellationTokenSource _stoppedSource = new();

    public AvaloniaApplicationLifeTime(IControlledApplicationLifetime avaloniaLifeTime)
    {
        _avaloniaLifeTime = avaloniaLifeTime;
        _avaloniaLifeTime.Exit += OnExit;
        
        // Signal that the application has started
        _startedSource?.Cancel();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        StopApplication();
    }

    public void StopApplication()
    {
        if (_stoppingSource is { IsCancellationRequested: true }) return;

        _stoppingSource.Cancel();
        Avalonia.Threading.Dispatcher.UIThread.Post(() => _avaloniaLifeTime.Shutdown());
        _stoppedSource.Cancel();
    }

    public CancellationToken ApplicationStarted => _startedSource.Token;
    public CancellationToken ApplicationStopping => _stoppingSource.Token;
    public CancellationToken ApplicationStopped => _stoppedSource.Token;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _startedSource.Dispose();
        _stoppingSource.Dispose();
        _stoppedSource.Dispose();
        _avaloniaLifeTime.Exit -= OnExit;
    }
}