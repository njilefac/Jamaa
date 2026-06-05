using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jamaa.Desktop.Services.Hosting;

public sealed partial class EmbeddedWebServer(ILogger<EmbeddedWebServer> logger) : IEmbeddedWebServer, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private WebApplication? _application;
    private Uri? _baseAddress;
    private readonly TaskCompletionSource<Uri> _startedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Uri BaseAddress => _baseAddress ?? throw new InvalidOperationException("The embedded web server has not started.");

    public int Port => BaseAddress.Port;

    public Task<Uri> Started => _startedSource.Task;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_application != null) return;

            _application = BuildApplication("", "");
            await _application.StartAsync(cancellationToken).ConfigureAwait(false);
            _baseAddress = ResolveBaseAddress(_application);
            _startedSource.TrySetResult(_baseAddress);
            LogEmbeddedServerStarted(_baseAddress);
        }
        catch (Exception ex)
        {
            _startedSource.TrySetException(ex);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_application == null) return;

            await _application.StopAsync(cancellationToken).ConfigureAwait(false);
            await _application.DisposeAsync().ConfigureAwait(false);
            _application = null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None).ConfigureAwait(false);
        _gate.Dispose();
    }

    private static WebApplication BuildApplication(string elsaManagementConnectionString, string elsaRuntimeConnectionString)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = [],
            ContentRootPath = AppContext.BaseDirectory
        });

        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));

        var application = builder.Build();
        application.MapGet("/", () => Results.Text("Jamaa embedded server"));
        application.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        return application;
    }

    private static Uri ResolveBaseAddress(WebApplication application)
    {
        var addresses = application.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()?
            .Addresses;

        var address = addresses?.FirstOrDefault()
                      ?? application.Urls.FirstOrDefault()
                      ?? throw new InvalidOperationException("The embedded web server did not expose a listening address.");

        return new Uri(address);
    }
}
