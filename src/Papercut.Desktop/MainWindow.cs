// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2026 Jaben Cargman
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


using System.Text.Json;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;

namespace Papercut.Desktop;

/// <summary>
/// The service's web UI in the OS webview. One window, reused; closing hides it to the tray.
/// While no service is available it shows a status panel with Retry instead.
/// </summary>
internal sealed class MainWindow : Window
{
    private readonly ShellSettings _settings;

    private readonly NativeWebView _webView;

    private readonly Border _offlinePanel;

    private readonly TextBlock _offlineText;

    private readonly Action _retry;

    private Uri? _origin;

    private bool _loaded;

    private bool _allowClose;

    public MainWindow(ShellSettings settings, Action retry)
    {
        _settings = settings;

        Title = "Papercut SMTP";
        Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Papercut/Assets/papercut.ico")));
        Width = Math.Max(settings.WindowWidth, 640);
        Height = Math.Max(settings.WindowHeight, 480);
        MinWidth = 480;
        MinHeight = 360;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        _webView = new NativeWebView { IsVisible = false };
        _webView.EnvironmentRequested += OnEnvironmentRequested;
        _webView.AdapterCreated += OnAdapterCreated;
        _webView.NavigationStarted += OnNavigationStarted;
        _webView.NewWindowRequested += OnNewWindowRequested;
        _webView.NavigationCompleted += (_, e) => _loaded = e.IsSuccess;

        _offlineText = new TextBlock
        {
            Text = "Starting Papercut...",
            FontSize = 16,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };

        var retryButton = new Button { Content = "Retry", HorizontalAlignment = HorizontalAlignment.Center };
        retryButton.Click += (_, _) => retry();

        _offlinePanel = new Border
        {
            Padding = new Thickness(32),
            Child = new StackPanel
            {
                Spacing = 16,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                MaxWidth = 520,
                Children = { _offlineText, retryButton }
            }
        };

        _retry = retry;

        // Windows draws the window's NativeMenu here; macOS puts it in the system menu bar
        var menuBar = new NativeMenuBar();
        DockPanel.SetDock(menuBar, Dock.Top);

        Content = new DockPanel { Children = { menuBar, new Panel { Children = { _webView, _offlinePanel } } } };
    }

    public event Action<Uri>? OpenExternallyRequested;

    /// <summary>
    /// Points the window at a service, or shows the status panel when there is none.
    /// </summary>
    public void SetService(Uri? origin, string status)
    {
        if (origin == null)
        {
            _origin = null;
            _loaded = false;
            _offlineText.Text = status;
            _offlinePanel.IsVisible = true;
            _webView.IsVisible = false;
            return;
        }

        _offlinePanel.IsVisible = false;
        _webView.IsVisible = true;

        if (_origin == origin) return;

        _origin = origin;
        _loaded = false;
        _webView.Source = origin;
    }

    /// <summary>
    /// Opens a message: routed in place when the UI is loaded, otherwise by loading its url.
    /// </summary>
    public void ShowMessage(string messageId)
    {
        if (_origin == null) return;

        var path = "/message/" + Uri.EscapeDataString(messageId);

        if (_loaded)
        {
            // utils/desktop-shell.ts DESKTOP_NAVIGATE_EVENT
            _ = _webView.InvokeScript(
                $"window.dispatchEvent(new CustomEvent('papercut-navigate', {{ detail: {JsonSerializer.Serialize(path)} }}))");
            return;
        }

        _webView.Source = new Uri(_origin, path.TrimStart('/'));
    }

    public void SetMenu(NativeMenu menu) => NativeMenu.SetMenu(this, menu);

    /// <summary>
    /// Reloads the web UI, or retries finding a service while there is none.
    /// </summary>
    public void Reload()
    {
        if (_origin == null)
        {
            _retry();
            return;
        }

        _webView.Refresh();
    }

    public void AllowClose() => _allowClose = true;

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        _settings.WindowWidth = (int)Width;
        _settings.WindowHeight = (int)Height;
        _settings.Save();

        if (!_allowClose)
        {
            e.Cancel = true;
            Hide();
        }

        base.OnClosing(e);
    }

    private void OnEnvironmentRequested(object? sender, WebViewEnvironmentRequestedEventArgs e)
    {
        e.EnableDevTools = Environment.GetEnvironmentVariable("PAPERCUT_DEVTOOLS") == "1";

        if (e is WindowsWebView2EnvironmentRequestedEventArgs windows)
            windows.UserDataFolder = ShellPaths.WebViewDataDirectory;
    }

    private void OnAdapterCreated(object? sender, WebViewAdapterEventArgs e)
    {
        // the web UI checks this to leave notifications to the shell (utils/desktop-shell.ts)
        try
        {
            var userAgent = _webView.UserAgent;
            if (!string.IsNullOrEmpty(userAgent) && !userAgent.Contains("PapercutDesktop/"))
                _webView.UserAgent = $"{userAgent} PapercutDesktop/8";
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Unable to tag the webview user agent");
        }

#if WINDOWS
        Platform.Windows.WebView2Hooks.Attach(_webView, IsAllowed);
#endif
    }

    private void OnNavigationStarted(object? sender, WebViewNavigationStartingEventArgs e)
    {
        if (e.Request == null || IsAllowed(e.Request)) return;

        e.Cancel = true;
        Handle(e.Request);
    }

    private void OnNewWindowRequested(object? sender, WebViewNewWindowRequestedEventArgs e)
    {
        // never a webview popup: links, raw downloads and sections all go to the browser
        e.Handled = true;

        if (e.Request == null) return;

        if (_origin != null && LinkPolicy.IsSameOrigin(e.Request, _origin))
            OpenExternallyRequested?.Invoke(e.Request);
        else
            Handle(e.Request);
    }

    private bool IsAllowed(Uri uri) => LinkPolicy.Decide(uri, _origin) == LinkAction.Allow;

    private void Handle(Uri uri)
    {
        switch (LinkPolicy.Decide(uri, _origin))
        {
            case LinkAction.OpenExternally:
                OpenExternallyRequested?.Invoke(uri);
                break;
            case LinkAction.Block:
                Log.Information("Blocked navigation to {Uri}", uri);
                break;
        }
    }
}
