using System;
using System.Threading;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Hosting;

namespace Libota.Desktop.Configuration.Hosting;

public class AvaloniaApplicationLifeTime(IControlledApplicationLifetime avaloniaLifeTime) : IHostApplicationLifetime
{
    public void StopApplication()
    {
        avaloniaLifeTime.Shutdown();
    }

    public CancellationToken ApplicationStarted => throw new NotImplementedException();
    public CancellationToken ApplicationStopping => throw new NotImplementedException();
    public CancellationToken ApplicationStopped => throw new NotImplementedException();
}