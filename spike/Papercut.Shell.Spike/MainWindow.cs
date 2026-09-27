using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Papercut.Shell.Spike;

/// <summary>
/// The window the shell will show: the service's web UI in a NativeWebView, with the link policy
/// from docs/plans/v8-desktop.md applied to every navigation, user-initiated or not.
/// </summary>
internal sealed class MainWindow : Window
{
    private readonly Uri _origin;

    private readonly bool _enableDevTools;

    private readonly NativeWebView _webView;

    private bool _allowClose;

    public MainWindow(Uri origin, bool enableDevTools)
    {
        _origin = origin;
        _enableDevTools = enableDevTools;

        Title = "Papercut shell spike";
        Width = 1000;
        Height = 820;

        var logBox = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("Cascadia Mono,Consolas,Menlo,monospace"),
            FontSize = 11,
            Height = 230
        };

        SpikeLog.Line += line => Dispatcher.UIThread.Post(() =>
        {
            logBox.Text += line + Environment.NewLine;
            logBox.CaretIndex = logBox.Text.Length;
        });

        _webView = new NativeWebView();
        _webView.EnvironmentRequested += OnEnvironmentRequested;
        _webView.AdapterCreated += OnAdapterCreated;
        _webView.NavigationStarted += OnNavigationStarted;
        _webView.NewWindowRequested += OnNewWindowRequested;
        _webView.NavigationCompleted += (_, e) =>
            SpikeLog.Write("NavigationCompleted", e.IsSuccess ? "ok" : "failed", e.Request?.ToString() ?? "");

        var header = new TextBlock
        {
            Margin = new Avalonia.Thickness(8, 4),
            Text = $"Click each case; the log below records the decision. Log file: {SpikeLog.FilePath}",
            TextWrapping = TextWrapping.Wrap
        };

        var dock = new DockPanel();
        DockPanel.SetDock(header, Dock.Top);
        DockPanel.SetDock(logBox, Dock.Bottom);
        dock.Children.Add(header);
        dock.Children.Add(logBox);
        dock.Children.Add(_webView);
        Content = dock;

        _webView.Source = origin;
    }

    public void AllowClose() => _allowClose = true;

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // closing hides to the tray; only the tray's Exit quits
        if (!_allowClose)
        {
            e.Cancel = true;
            Hide();
            SpikeLog.Write("window", "hidden", "close hides to tray");
        }

        base.OnClosing(e);
    }

    private void OnEnvironmentRequested(object? sender, WebViewEnvironmentRequestedEventArgs e)
    {
        e.EnableDevTools = _enableDevTools;

        var dataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Papercut.Shell.Spike");

        switch (e)
        {
            case WindowsWebView2EnvironmentRequestedEventArgs win:
                win.UserDataFolder = Path.Combine(dataRoot, "WebView2");
                break;
            case GtkWebViewEnvironmentRequestedEventArgs gtk:
                gtk.BaseDataDirectory = Path.Combine(dataRoot, "webkitgtk");
                gtk.BaseCacheDirectory = Path.Combine(dataRoot, "webkitgtk-cache");
                break;
        }

        SpikeLog.Write("EnvironmentRequested", "config", $"{e.GetType().Name} devtools={e.EnableDevTools} data={dataRoot}");
    }

    private void OnAdapterCreated(object? sender, WebViewAdapterEventArgs e)
    {
        SpikeLog.Write("AdapterCreated", "info", $"{_webView.AdapterInfo}");

        if (OperatingSystem.IsWindows())
        {
            WebView2Hooks.Attach(_webView, IsAllowed);
        }
    }

    private void OnNavigationStarted(object? sender, WebViewNavigationStartingEventArgs e)
    {
        var allowed = e.Request is not null && IsAllowed(e.Request);
        e.Cancel = !allowed;

        SpikeLog.Write("NavigationStarted", allowed ? "allowed" : "CANCELLED", Describe(e.Request));
    }

    private void OnNewWindowRequested(object? sender, WebViewNewWindowRequestedEventArgs e)
    {
        // never open a webview popup; the real shell hands http(s) to the default browser
        e.Handled = true;

        SpikeLog.Write("NewWindowRequested", "HANDLED", Describe(e.Request));
    }

    private bool IsAllowed(Uri uri) =>
        uri.Scheme is "about"
        || ((uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && string.Equals(uri.Authority, _origin.Authority, StringComparison.OrdinalIgnoreCase));

    private static string Describe(Uri? uri) => uri is null ? "(no url)" : $"{uri} {CaseOf(uri)}";

    private static string CaseOf(Uri uri)
    {
        var query = uri.Query;
        var index = query.IndexOf("case=", StringComparison.Ordinal);

        if (index >= 0) return $"<{query[(index + 5)..]}>";

        return uri.Scheme switch
        {
            "mailto" => "<T5>",
            "file" => "<T6>",
            _ => ""
        };
    }
}
