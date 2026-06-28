using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Jamaa.Desktop.Services.Hosting;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting;

public partial class AutomationRulesViewModel : ObservableObject, IApplicationModule, IRouteableViewModel
{
    private readonly ILogger<AutomationRulesViewModel> _logger;

    [ObservableProperty] private string? _elsaServerErrorMessage;
    [ObservableProperty] private bool _hasElsaServerError;
    [ObservableProperty] private bool _isElsaServerReady;
    [ObservableProperty] private string? _elsaServerUrl;
    [ObservableProperty] private bool _showStatusMessage = true;
    [ObservableProperty] private string _statusMessage = "Starting embedded Elsa Server...";

    public AutomationRulesViewModel(
        IEmbeddedWebServer embeddedWebServer,
        ILogger<AutomationRulesViewModel> logger)
    {
        _logger = logger;
        _ = LoadElsaServerUrlAsync(embeddedWebServer);
    }

    public Guid Id => Guid.Parse("29315df6-29e2-40f9-81ac-8b3431df7b1a");
    public string Title => "Automation Rules";
    public object? HeaderContent => null;

    private async Task LoadElsaServerUrlAsync(IEmbeddedWebServer embeddedWebServer)
    {
        try
        {
            var address = await embeddedWebServer.Started.ConfigureAwait(false);
            var serverUrl = $"{address.AbsoluteUri}elsa/api/";
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ElsaServerUrl = serverUrl;
                IsElsaServerReady = true;
                ShowStatusMessage = false;
                HasElsaServerError = false;
                StatusMessage = string.Empty;
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to resolve embedded Elsa Server URL for automation rules.");
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ElsaServerErrorMessage = "Unable to load embedded Elsa Server.";
                HasElsaServerError = true;
                ShowStatusMessage = false;
                StatusMessage = string.Empty;
            });
        }
    }
}