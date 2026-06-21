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

    [ObservableProperty] private string? _elsaStudioErrorMessage;
    [ObservableProperty] private bool _hasElsaStudioError;
    [ObservableProperty] private bool _isElsaStudioReady;
    [ObservableProperty] private string? _elsaStudioUrl;
    [ObservableProperty] private bool _showStatusMessage = true;
    [ObservableProperty] private string _statusMessage = "Starting embedded Elsa Studio...";

    public AutomationRulesViewModel(
        IEmbeddedWebServer embeddedWebServer,
        ILogger<AutomationRulesViewModel> logger)
    {
        _logger = logger;
        _ = LoadElsaStudioUrlAsync(embeddedWebServer);
    }

    public Guid Id => Guid.Parse("29315df6-29e2-40f9-81ac-8b3431df7b1a");
    public string Title => "Automation Rules";
    public object? HeaderContent => null;

    private async Task LoadElsaStudioUrlAsync(IEmbeddedWebServer embeddedWebServer)
    {
        try
        {
            var address = await embeddedWebServer.Started.ConfigureAwait(false);
            var studioUrl = address.AbsoluteUri;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ElsaStudioUrl = studioUrl;
                IsElsaStudioReady = true;
                ShowStatusMessage = false;
                HasElsaStudioError = false;
                StatusMessage = string.Empty;
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to resolve embedded Elsa Studio URL for automation rules.");
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ElsaStudioErrorMessage = "Unable to load embedded Elsa Studio.";
                HasElsaStudioError = true;
                ShowStatusMessage = false;
                StatusMessage = string.Empty;
            });
        }
    }
}