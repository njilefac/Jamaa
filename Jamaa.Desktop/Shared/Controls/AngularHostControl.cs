using System;
using System.Diagnostics;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using WebViewControl;

namespace Jamaa.Desktop.Shared.Controls;

public class AngularHostControl : TemplatedControl
{
    public static readonly StyledProperty<string?> TargetUrlProperty =
        AvaloniaProperty.Register<AngularHostControl, string?>(nameof(TargetUrl));

    public static readonly StyledProperty<object?> InitialDataProperty =
        AvaloniaProperty.Register<AngularHostControl, object?>(nameof(InitialData));

    public static readonly StyledProperty<string> BootstrapFunctionNameProperty =
        AvaloniaProperty.Register<AngularHostControl, string>(nameof(BootstrapFunctionName), "initializeMicroApp");

    public static readonly StyledProperty<bool> InjectInitialDataOnNavigationProperty =
        AvaloniaProperty.Register<AngularHostControl, bool>(nameof(InjectInitialDataOnNavigation), true);

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private WebView? _webView;
    private string? _lastNavigatedUrl;

    static AngularHostControl()
    {
        TargetUrlProperty.Changed.AddClassHandler<AngularHostControl>((control, _) => control.ApplyTargetUrl());
        InitialDataProperty.Changed.AddClassHandler<AngularHostControl>((control, _) => control.InjectInitialData());
        BootstrapFunctionNameProperty.Changed.AddClassHandler<AngularHostControl>((control, _) => control.InjectInitialData());
    }

    public string? TargetUrl
    {
        get => GetValue(TargetUrlProperty);
        set => SetValue(TargetUrlProperty, value);
    }

    public object? InitialData
    {
        get => GetValue(InitialDataProperty);
        set => SetValue(InitialDataProperty, value);
    }

    public string BootstrapFunctionName
    {
        get => GetValue(BootstrapFunctionNameProperty);
        set => SetValue(BootstrapFunctionNameProperty, value);
    }

    public bool InjectInitialDataOnNavigation
    {
        get => GetValue(InjectInitialDataOnNavigationProperty);
        set => SetValue(InjectInitialDataOnNavigationProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        DetachWebViewEvents();

        _webView = e.NameScope.Find<WebView>("PART_WebView");
        if (_webView == null) return;

        _webView.Navigated += OnWebViewNavigated;
        ApplyTargetUrl();
    }

    private void DetachWebViewEvents()
    {
        if (_webView != null) _webView.Navigated -= OnWebViewNavigated;
    }

    private void ApplyTargetUrl()
    {
        if (_webView == null || string.IsNullOrWhiteSpace(TargetUrl)) return;
        if (string.Equals(_webView.Address, TargetUrl, StringComparison.Ordinal)) return;
        _webView.LoadUrl(TargetUrl, string.Empty);
    }

    private void OnWebViewNavigated(string url, string frameName)
    {
        if (!IsTopLevelFrame(frameName)) return;

        _lastNavigatedUrl = url;
        if (!InjectInitialDataOnNavigation) return;

        InjectInitialData();
    }

    private void InjectInitialData()
    {
        if (_webView == null || InitialData == null || string.IsNullOrWhiteSpace(_lastNavigatedUrl)) return;
        if (string.IsNullOrWhiteSpace(BootstrapFunctionName)) return;

        try
        {
            // The Angular side expects a JSON string payload argument.
            var serializedPayload = JsonSerializer.Serialize(InitialData, JsonSerializerOptions);
            var javascriptStringPayload = JsonSerializer.Serialize(serializedPayload, JsonSerializerOptions);
            var javascriptFunctionName = JsonSerializer.Serialize(BootstrapFunctionName, JsonSerializerOptions);

            var script = $$"""
                           (() => {
                             const functionName = {{javascriptFunctionName}};
                             const payload = {{javascriptStringPayload}};
                             const bootstrap = globalThis[functionName];
                             if (typeof bootstrap === "function") {
                               bootstrap(payload);
                             }
                           })();
                           """;

            _webView.ExecuteScript(script, _lastNavigatedUrl);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"AngularHostControl initialization script failed: {exception}");
        }
    }

    private static bool IsTopLevelFrame(string frameName)
    {
        return string.IsNullOrWhiteSpace(frameName) || string.Equals(frameName, "_top", StringComparison.OrdinalIgnoreCase);
    }
}
