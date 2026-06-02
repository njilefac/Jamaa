using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jamaa.Desktop.Services.Hosting;

public interface IEmbeddedWebServer
{
    Uri BaseAddress { get; }

    int Port { get; }

    Task<Uri> Started { get; }

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}
