using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Serilog;

namespace Jamaa.Desktop.Shared.Controls;

public class EmbeddedBrowser : ContentControl
{
    public static readonly StyledProperty<string?> TargetUrlProperty =
        AvaloniaProperty.Register<EmbeddedBrowser, string?>(nameof(TargetUrl));

    public static readonly StyledProperty<object?> InitialDataProperty =
        AvaloniaProperty.Register<EmbeddedBrowser, object?>(nameof(InitialData));

    public static readonly StyledProperty<string> BootstrapFunctionNameProperty =
        AvaloniaProperty.Register<EmbeddedBrowser, string>(nameof(BootstrapFunctionName), "initializeMicroApp");

    public static readonly StyledProperty<bool> InjectInitialDataOnNavigationProperty =
        AvaloniaProperty.Register<EmbeddedBrowser, bool>(nameof(InjectInitialDataOnNavigation), true);

    public static readonly StyledProperty<string> BrowserUnavailableMessageProperty =
        AvaloniaProperty.Register<EmbeddedBrowser, string>(nameof(BrowserUnavailableMessage),
            "Embedded browser runtime is unavailable.");

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly ILogger Logger = Log.ForContext<EmbeddedBrowser>();

    private NativeWebView? _webView;
    private Border? _fallbackMessage;

    static EmbeddedBrowser()
    {
        TargetUrlProperty.Changed.AddClassHandler<EmbeddedBrowser>((control, _) => control.ApplyTargetUrl());
        InitialDataProperty.Changed.AddClassHandler<EmbeddedBrowser>((control, _) => control.TriggerInitialDataInjection());
        BootstrapFunctionNameProperty.Changed.AddClassHandler<EmbeddedBrowser>((control, _) => control.TriggerInitialDataInjection());
        BrowserUnavailableMessageProperty.Changed.AddClassHandler<EmbeddedBrowser>((control, _) => control.ShowBrowserUnavailableMessage());
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

    public string BrowserUnavailableMessage
    {
        get => GetValue(BrowserUnavailableMessageProperty);
        set => SetValue(BrowserUnavailableMessageProperty, value);
    }

    public EmbeddedBrowser()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
        MinHeight = 320;
        ClipToBounds = true;
        BuildContent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ApplyTargetUrl();
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Content is Control child)
        {
            child.Arrange(new Rect(finalSize));
        }
        return base.ArrangeOverride(finalSize);
    }

    private void ApplyTargetUrl()
    {
        if (_webView == null || string.IsNullOrWhiteSpace(TargetUrl)) return;
        if (!Uri.TryCreate(TargetUrl, UriKind.Absolute, out var targetUri)) return;
        if (_webView.Source == targetUri) return;
        _webView.Navigate(targetUri);
    }

    private void OnWebViewNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs eventArgs)
    {
        if (!eventArgs.IsSuccess)
        {
            Logger.Error("EmbeddedBrowser navigation failed for {TargetUrl}", TargetUrl);
            return;
        }

        TriggerLayoutStabilizerInjection();
        TriggerErrorBridgeInjection();
        if (!InjectInitialDataOnNavigation) return;
        TriggerInitialDataInjection();
    }

    private void OnWebViewMessageReceived(object? sender, WebMessageReceivedEventArgs eventArgs)
    {
        if (string.IsNullOrWhiteSpace(eventArgs.Body)) return;

        try
        {
            using var payload = JsonDocument.Parse(eventArgs.Body);
            var root = payload.RootElement;

            if (!root.TryGetProperty("origin", out var origin) ||
                !string.Equals(origin.GetString(), "web-app-host", StringComparison.Ordinal))
            {
                return;
            }

            var kind = GetOptionalString(root, "kind") ?? "error";
            var message = GetOptionalString(root, "message") ?? "Unknown web-app runtime error.";
            var stack = GetOptionalString(root, "stack");
            var source = GetOptionalString(root, "source");
            var line = GetOptionalInt(root, "line");
            var column = GetOptionalInt(root, "column");

            Logger.Error(
                "web-app runtime error ({Kind}) at {TargetUrl}. Message: {Message}. Source: {Source} ({Line}:{Column}). Stack: {Stack}",
                kind, TargetUrl, message, source, line, column, stack);
        }
        catch (Exception exception)
        {
            Logger.Warning(exception, "EmbeddedBrowser failed to parse web message payload: {Payload}", eventArgs.Body);
        }
    }

    private static string? GetOptionalString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var propertyValue) && propertyValue.ValueKind != JsonValueKind.Null
            ? propertyValue.GetString()
            : null;
    }

    private static int? GetOptionalInt(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var propertyValue) && propertyValue.TryGetInt32(out var value)
            ? value
            : null;
    }

    private void TriggerErrorBridgeInjection()
    {
        _ = InstallErrorBridgeAsync();
    }

    private void TriggerInitialDataInjection()
    {
        _ = InjectInitialDataAsync();
    }

    private void TriggerLayoutStabilizerInjection()
    {
        _ = StabilizeEmbeddedLayoutAsync();
    }

    private async Task InstallErrorBridgeAsync()
    {
        if (_webView == null) return;

        const string script = """
                              (() => {
                                if (globalThis.__jamaaEmbeddedWebErrorBridgeInstalled) return;
                                globalThis.__jamaaEmbeddedWebErrorBridgeInstalled = true;

                                const post = payload => {
                                  try {
                                    if (typeof globalThis.invokeCSharpAction === "function") {
                                      globalThis.invokeCSharpAction(JSON.stringify(payload));
                                    }
                                  } catch {
                                    // ignored on purpose; host-side logging captures bridge issues.
                                  }
                                };

                                const toText = value => {
                                  if (typeof value === "string") return value;
                                  if (value?.message) return value.message;
                                  try { return JSON.stringify(value); } catch { return String(value); }
                                };

                                globalThis.addEventListener("error", event => {
                                  post({
                                    origin: "web-app-host",
                                    kind: "error",
                                    message: toText(event.message),
                                    source: event.filename ?? null,
                                    line: event.lineno ?? null,
                                    column: event.colno ?? null,
                                    stack: event.error?.stack ?? null
                                  });
                                });

                                globalThis.addEventListener("unhandledrejection", event => {
                                  const reason = event.reason;
                                  post({
                                    origin: "web-app-host",
                                    kind: "unhandledrejection",
                                    message: toText(reason),
                                    stack: reason?.stack ?? null
                                  });
                                });

                                const originalConsoleError = console.error?.bind(console);
                                console.error = (...args) => {
                                  post({
                                    origin: "web-app-host",
                                    kind: "console-error",
                                    message: args.map(toText).join(" ")
                                  });
                                  originalConsoleError?.(...args);
                                };
                              })();
                              """;

        try
        {
            await _webView.InvokeScript(script);
        }
        catch (Exception exception)
        {
            Logger.Warning(exception, "EmbeddedBrowser failed to install JavaScript error bridge for {TargetUrl}", TargetUrl);
        }
    }

    private async Task StabilizeEmbeddedLayoutAsync()
    {
        if (_webView == null) return;

        const string script = """
                              (() => {
                                const ensureViewportRoot = () => {
                                  const app = document.getElementById("app");
                                  document.documentElement.style.height = "100%";
                                  document.documentElement.style.width = "100%";
                                  document.body.style.height = "100%";
                                  document.body.style.width = "100%";
                                  document.body.style.margin = "0";
                                  document.body.style.overflow = "hidden";
                                  if (app) {
                                    app.style.height = "100%";
                                    app.style.width = "100%";
                                    app.style.overflow = "hidden";
                                  }
                                };

                                ensureViewportRoot();
                                globalThis.setTimeout(ensureViewportRoot, 0);
                                globalThis.setTimeout(ensureViewportRoot, 250);
                              })();
                              """;

        try
        {
            await _webView.InvokeScript(script);
        }
        catch (Exception exception)
        {
            Logger.Warning(exception, "EmbeddedBrowser failed to inject layout stabilizer script for {TargetUrl}", TargetUrl);
        }
    }

    private async Task InjectInitialDataAsync()
    {
        if (_webView == null || InitialData == null) return;
        if (string.IsNullOrWhiteSpace(BootstrapFunctionName)) return;

        try
        {
            // The web-app side expects a JSON string payload argument.
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

            await _webView.InvokeScript(script);
        }
        catch (Exception exception)
        {
            Logger.Error(exception, "EmbeddedBrowser initialization script failed for {TargetUrl}", TargetUrl);
            Debug.WriteLine($"EmbeddedBrowser initialization script failed: {exception}");
        }
    }

    private void BuildContent()
    {
        try
        {
            _webView = new NativeWebView
            {
                Source = new Uri("about:blank"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                IsHitTestVisible = true
            };
            _webView.NavigationCompleted += OnWebViewNavigationCompleted;
            _webView.WebMessageReceived += OnWebViewMessageReceived;
            Content = _webView;
            ApplyTargetUrl();
        }
        catch (Exception exception)
        {
            Logger.Error(exception, "EmbeddedBrowser failed to initialize web view.");
            Debug.WriteLine($"EmbeddedBrowser failed to initialize WebView: {exception}");
            _webView = null;
            ShowBrowserUnavailableMessage();
        }
    }

    private void ShowBrowserUnavailableMessage()
    {
        if (_webView != null) return;

        _fallbackMessage = new Border
        {
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.IndianRed,
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12),
            Child = new TextBlock
            {
                Text = BrowserUnavailableMessage,
                Foreground = Brushes.IndianRed,
                TextWrapping = TextWrapping.Wrap
            }
        };
        Content = _fallbackMessage;
    }
}
