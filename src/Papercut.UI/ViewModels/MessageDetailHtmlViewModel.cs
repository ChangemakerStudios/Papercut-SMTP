// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
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


using System.Runtime.InteropServices;

using Microsoft.Web.WebView2.Core;

using Papercut.AppLayer.Processes;
using Papercut.Core.Infrastructure.Logging;
using Papercut.Domain.HtmlPreviews;
using Papercut.Helpers;
using Papercut.Infrastructure.WebView;
using Papercut.Views;

namespace Papercut.ViewModels;

using System.Text.Json;

public class MessageDetailHtmlViewModel : Screen, IMessageDetailItem, IHandle<SettingsUpdatedEvent>
{
    private readonly ILogger _logger;

    private readonly IHtmlPreviewGenerator _previewGenerator;

    private readonly WebView2Information _webView2Information;

    private readonly ProcessService _processService;

    private CoreWebView2? _coreWebView;

    private string? _htmlFile;

    private bool _isWebViewInstalled = false;

    private readonly SettingsSaveDebouncer<double> _zoomSaveDebouncer;

    public MessageDetailHtmlViewModel(
        ILogger logger,
        WebView2Information webView2Information,
        ProcessService processService,
        IHtmlPreviewGenerator previewGenerator)
    {
        DisplayName = "Message";
        _logger = logger;
        _webView2Information = webView2Information;
        _processService = processService;
        _previewGenerator = previewGenerator;
        IsWebViewInstalled = _webView2Information.IsInstalled;

        // Set up debounced zoom save to reduce I/O during rapid zoom changes
        _zoomSaveDebouncer = new SettingsSaveDebouncer<double>(newZoom =>
        {
            Settings.Default.HtmlViewZoomFactor = newZoom;
            Settings.Default.Save();
        });
    }

    public bool IsWebViewInstalled
    {

        get => _isWebViewInstalled;

        set
        {
            _isWebViewInstalled = value;
            NotifyOfPropertyChange(() => IsWebViewInstalled);
            NotifyOfPropertyChange(() => ShowHtmlView);
        }
    }

    public string? HtmlFile
    {

        get => _htmlFile;

        set
        {
            _htmlFile = value;
            NotifyOfPropertyChange(() => HtmlFile);
            NotifyOfPropertyChange(() => ShowHtmlView);
        }
    }

    public bool ShowHtmlView => !string.IsNullOrWhiteSpace(HtmlFile);

    public MessageDetailAttachmentsViewModel? AttachmentsViewModel =>
        (this.Parent as MessageDetailViewModel)?.AttachmentsViewModel;

    public async Task HandleAsync(SettingsUpdatedEvent settingsEvent, CancellationToken cancellationToken)
    {
        // Check if SSL certificate setting changed
        if (settingsEvent.PreviousSettings.IgnoreSslCertificateErrors != settingsEvent.NewSettings.IgnoreSslCertificateErrors)
        {
            _logger.Information(
                "SSL Certificate Error setting changed from {Old} to {New}. Restart Papercut for changes to take effect.",
                settingsEvent.PreviousSettings.IgnoreSslCertificateErrors,
                settingsEvent.NewSettings.IgnoreSslCertificateErrors);
        }

        await Task.CompletedTask;
    }

    public void ShowMessage(MimeMessage? mailMessageEx)
    {
        ArgumentNullException.ThrowIfNull(mailMessageEx);

        try
        {
            HtmlFile = _previewGenerator.GetHtmlPreviewFile(mailMessageEx);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failure Saving Browser Temp File for {MailMessage}", mailMessageEx.ToString());
        }
    }

    private bool IsLocalNavigation(string navigateToUrl)
    {
        if (string.IsNullOrEmpty(navigateToUrl))
        {
            return true;
        }

        // Allow about: and data: URIs to be handled by WebView2
        if (navigateToUrl.StartsWith("about:") || navigateToUrl.StartsWith("data:text/html"))
        {
            return true;
        }

        // Check if it's a file:/// URI
        if (navigateToUrl.StartsWith("file:"))
        {
            // Allow navigation to our own HTML preview files and their resources
            // The HTML preview file is stored in a Papercut-* temp directory
            if (!string.IsNullOrEmpty(_htmlFile))
            {
                try
                {
                    // Get the directory of our HTML preview file (e.g., C:\Users\...\Temp\Papercut-abc123)
                    var htmlFileDir = Path.GetDirectoryName(_htmlFile);

                    if (!string.IsNullOrEmpty(htmlFileDir))
                    {
                        // Convert file:/// URL to local path for comparison
                        var uri = new Uri(navigateToUrl);
                        var localPath = uri.LocalPath;

                        // Check if the target file is in our preview directory or subdirectory
                        // This allows the main HTML file and any embedded resources (images, CSS, etc.)
                        var targetDir = Path.GetDirectoryName(localPath);

                        if (!string.IsNullOrEmpty(targetDir))
                        {
                            // Normalize both paths to prevent traversal attacks
                            var normalizedHtmlDir = Path.GetFullPath(htmlFileDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                            var normalizedTargetDir = Path.GetFullPath(targetDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                            // Check if target is within preview directory using normalized paths
                            if (normalizedTargetDir.Equals(normalizedHtmlDir, StringComparison.OrdinalIgnoreCase) ||
                                normalizedTargetDir.StartsWith(normalizedHtmlDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Error checking if file:/// URI is local navigation: {Url}", navigateToUrl);
                }
            }

            // All other file:/// links should be opened externally with the shell
            return false;
        }

        return false;
    }

    private sealed record ZoomInfoFromJavascript(string Type, string Direction);

    protected override void OnViewLoaded(object view)
    {
        base.OnViewLoaded(view);

        if (view is not MessageDetailHtmlView typedView)
        {
            _logger.Error("Unable to locate the MessageDetailHtmlView to hook the WebBrowser Control");
            return;
        }

        typedView.htmlView.CoreWebView2InitializationCompleted += async (_, args) =>
        {
            if (!args.IsSuccess)
            {
                _logger.Error(
                    args.InitializationException,
                    "Failure Initializing Edge WebView2");

            }
            else
            {
                _coreWebView = typedView.htmlView.CoreWebView2;
                await SetupWebView(_coreWebView, typedView.htmlView);

                // Restore saved zoom level
                typedView.htmlView.ZoomFactor = Settings.Default.HtmlViewZoomFactor;
            }
        };

        if (!typedView.IsEnabled)
        {
            typedView.htmlView.Visibility = Visibility.Collapsed;
        }

        Observable
            .FromEvent<DependencyPropertyChangedEventHandler,
                DependencyPropertyChangedEventArgs>(
                a => (_, e) => a(e),
                h => typedView.IsEnabledChanged += h,
                h => typedView.IsEnabledChanged -= h)
            .Throttle(TimeSpan.FromMilliseconds(100))
            .Select(args => args.NewValue.ToType<bool>()
                ? Visibility.Visible
                : Visibility.Collapsed)
            .ObserveOn(Dispatcher.CurrentDispatcher)
            .Subscribe((newState) =>
            {
                if (typedView.htmlView.Visibility != newState)
                {
                    typedView.htmlView.Visibility = newState;
                }
            });

        // Context menu is now handled by CoreWebView2.ContextMenuRequested in SetupWebView
        // Removed the ContextMenuOpening handler that was blocking all context menus
    }

    private async Task SetupWebView(CoreWebView2 coreWebView, WebView2Base typedViewHtmlView)
    {
        // Enable browser accelerator keys (Ctrl+F, Ctrl+P, etc.) for better UX
        coreWebView.Settings.AreBrowserAcceleratorKeysEnabled = true;

        // Handle SSL certificate errors if the setting is enabled
        _logger.Information("WebView2 SSL Certificate Error Handling: {Enabled}", Settings.Default.IgnoreSslCertificateErrors ? "Enabled" : "Disabled");

        if (Settings.Default.IgnoreSslCertificateErrors)
        {
            coreWebView.ServerCertificateErrorDetected += (sender, args) =>
            {
                var deferral = args.GetDeferral();
                try
                {
                    _logger.Information(
                        "SSL certificate error detected and ignored for {Uri} - Status: {Status}",
                        args.RequestUri,
                        args.ErrorStatus);

                    args.Action = CoreWebView2ServerCertificateErrorAction.AlwaysAllow;
                }
                finally
                {
                    deferral.Complete();
                }
            };
        }
        else
        {
            coreWebView.ServerCertificateErrorDetected += (sender, args) =>
            {
                var deferral = args.GetDeferral();
                try
                {
                    _logger.Warning(
                        "SSL certificate error detected and BLOCKED for {Uri} - Status: {Status}",
                        args.RequestUri,
                        args.ErrorStatus);
                    // Default action is to block (Cancel)
                    args.Action = CoreWebView2ServerCertificateErrorAction.Cancel;
                }
                finally
                {
                    deferral.Complete();
                }
            };
        }

        // Handle context menu for links
        coreWebView.ContextMenuRequested += (sender, args) =>
        {
            var deferral = args.GetDeferral();
            try
            {
                // Check if the context menu is for a link
                string? linkUrl = null;

                try
                {
                    linkUrl = args.ContextMenuTarget.LinkUri;
                }
                catch (COMException ex) when (ex.HResult == unchecked((int)0x8000000E))
                {
                    // The ContextMenuTarget became invalid (race condition if user moved quickly)
                    _logger.Debug("Context menu target became invalid (user moved quickly)");
                    args.MenuItems.Clear();
                    return;
                }

                if (!string.IsNullOrEmpty(linkUrl))
                {
                    // Remove default menu items
                    args.MenuItems.Clear();

                    // Add "Open Link" menu item
                    var openLinkItem = coreWebView.Environment.CreateContextMenuItem(
                        "Open Link",
                        null,
                        CoreWebView2ContextMenuItemKind.Command);

                    openLinkItem.CustomItemSelected += async (_, __) =>
                    {
                        try
                        {
                            await HandleLinkNavigationAsync(new Uri(linkUrl));
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "Failed to open link from context menu: {Url}", linkUrl);
                        }
                    };

                    args.MenuItems.Add(openLinkItem);

                    // Add "Copy Link" menu item
                    var copyLinkItem = coreWebView.Environment.CreateContextMenuItem(
                        "Copy Link",
                        null,
                        CoreWebView2ContextMenuItemKind.Command);

                    copyLinkItem.CustomItemSelected += (_, __) =>
                    {
                        CopyLinkToClipboard(linkUrl);
                    };

                    args.MenuItems.Add(copyLinkItem);

                    _logger.Debug("Context menu shown for link: {Url}", linkUrl);
                }
                else
                {
                    // For non-links, remove all menu items (no context menu)
                    args.MenuItems.Clear();
                }
            }
            finally
            {
                deferral.Complete();
            }
        };

        coreWebView.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);

        var externalNavigation = new List<string>();

        coreWebView.WebResourceRequested += async (_, args) =>
        {
            if (externalNavigation.Contains(args.Request.Uri))
            {
                // handle the response
                var response =
                    coreWebView.Environment.CreateWebResourceResponse(new MemoryStream(), 200, "OK",
                        "Content-Type: text/html");
                args.Response = response;

                externalNavigation.Remove(args.Request.Uri);
            }
        };

        coreWebView.NewWindowRequested += async (_, args) =>
        {
            var internalUrl = !args.IsUserInitiated || IsLocalNavigation(args.Uri);

            if (internalUrl)
            {
                args.Handled = false;
                return;
            }

            // external navigation
            args.Handled = true;

            try
            {
                await HandleLinkNavigationAsync(new Uri(args.Uri));
            }
            catch (Exception ex) when (_logger.ErrorWithContext(ex, "Failure Navigating to External Url {Url}", args.Uri))
            {
            }
        };

        coreWebView.NavigationStarting += async (_, args) =>
        {
            var uri = args.Uri;

            var internalUrl = !args.IsUserInitiated || IsLocalNavigation(uri);

            if (internalUrl)
            {
                args.Cancel = false;
                return;
            }

            externalNavigation.Add(uri);

            // external navigation
            args.Cancel = true;

            try
            {
                await HandleLinkNavigationAsync(new Uri(uri));
            }
            catch (Exception ex) when (_logger.ErrorWithContext(ex, "Failure Navigating to External Url {Url}", uri))
            {
            }
        };

        coreWebView.WebMessageReceived += async (sender, args) =>
        {
            try
            {
                var json = args.TryGetWebMessageAsString();

                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.Warning("Received empty or null web message");
                    return;
                }

                var data = JsonSerializer.Deserialize<ZoomInfoFromJavascript>(json);

                if (data is null)
                {
                    _logger.Warning("Failed to deserialize web message: {Json}", json);
                    return;
                }

                _logger.Verbose("Received Web Message {@Message}", data);

                if (data.Type == "zoom")
                {
                    double zoomDelta = data.Direction == "in"
                        ? ZoomHelper.WebView2Zoom.Increment
                        : -ZoomHelper.WebView2Zoom.Increment;
                    double newZoom = typedViewHtmlView.ZoomFactor + zoomDelta;

                    newZoom = Math.Max(
                        ZoomHelper.WebView2Zoom.MinZoom,
                        Math.Min(ZoomHelper.WebView2Zoom.MaxZoom, newZoom));

                    typedViewHtmlView.ZoomFactor = newZoom;

                    // Debounce settings save to reduce I/O during rapid zoom changes
                    _zoomSaveDebouncer.OnValueChanged(newZoom);

                    // Show zoom indicator in WebView2
                    var percentage = (int)Math.Round(newZoom * 100);
                    await coreWebView.ExecuteScriptAsync($"window.showPapercutZoom && window.showPapercutZoom({percentage});");
                }
            }
            catch (JsonException ex)
            {
                _logger.Error(ex, "Failed to parse web message as JSON");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unexpected error handling web message");
            }
        };

        await coreWebView.AddScriptToExecuteOnDocumentCreatedAsync(@"
            document.addEventListener('DOMContentLoaded', function() {
                var style = document.createElement('style');
                style.textContent = '#papercut-zoom-indicator { position: fixed; top: 50%; left: 50%; background: rgba(0, 0, 0, 0.88); color: white; font-size: 24px; font-weight: 600; padding: 16px 24px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0, 0, 0, 0.5); z-index: 999999; opacity: 0; transition: opacity 0.15s ease-out; pointer-events: none; font-family: sans-serif; }';
                document.head.appendChild(style);

                var indicator = document.createElement('div');
                indicator.id = 'papercut-zoom-indicator';
                document.body.appendChild(indicator);

                var fadeTimeout;
                window.showPapercutZoom = function(percentage) {
                    var currentZoom = percentage / 100;
                    var inverseZoom = 1 / currentZoom;
                    indicator.style.transform = 'translate(-50%, -50%) scale(' + inverseZoom + ')';
                    indicator.textContent = percentage + '%';
                    indicator.style.opacity = '1';
                    clearTimeout(fadeTimeout);
                    fadeTimeout = setTimeout(function() {
                        indicator.style.transition = 'opacity 0.3s ease-in';
                        indicator.style.opacity = '0';
                    }, 800);
                };
            });
        ");

        await coreWebView.AddScriptToExecuteOnDocumentCreatedAsync(@"
            window.addEventListener('wheel', function(e) {
                if (e.ctrlKey && window?.chrome?.webview?.postMessage) {
                    e.preventDefault();
                    window.chrome.webview.postMessage(JSON.stringify({
                        Type: 'zoom',
                        Direction: e.deltaY < 0 ? 'in' : 'out'
                    }));
                }
            }, { passive: false });
        ");

        coreWebView.DisableEdgeFeatures();

        this.GetPropertyValues(p => p.HtmlFile)
            .Subscribe(
                file =>
                {
                    if (file.IsNullOrWhiteSpace())
                    {
                        coreWebView.NavigateToString(string.Empty);
                    }
                    else
                    {
                        coreWebView.Navigate($"file://{file.Replace("/", @"\")}");
                    }
                }
            );
    }

    private async Task HandleLinkNavigationAsync(Uri navigateToUri)
    {
        if (navigateToUri.Scheme == Uri.UriSchemeHttp || navigateToUri.Scheme == Uri.UriSchemeHttps)
        {
            _processService.OpenUri(navigateToUri);
        }
        else if (navigateToUri.Scheme == Uri.UriSchemeFile)
        {
            // Handle file:/// links - open with shell/explorer using ProcessService
            var localPath = navigateToUri.LocalPath;

            // Check if the path is a directory
            if (Directory.Exists(localPath))
            {
                _processService.OpenFolder(localPath);
                _logger.Information("Opened directory in Explorer: {Path}", localPath);
            }
            else if (File.Exists(localPath))
            {
                var result = _processService.OpenFile(localPath);
                if (result.IsSuccess)
                {
                    _logger.Information("Opened file with associated application: {Path}", localPath);
                }
                else
                {
                    _logger.Error("Failed to open file: {Path}. Errors: {Errors}", localPath, string.Join(", ", result.Errors));
                }
            }
            else
            {
                _logger.Warning("File or directory not found: {Path}", localPath);
            }
        }
        else if (navigateToUri.Scheme.Equals("cid", StringComparison.OrdinalIgnoreCase))
        {
            // Navigate to the attachment in the parts view
            var model = await this.GetConductor().ActivateViewModelOf<MessageDetailPartsListViewModel>();
            var part = model.Parts.FirstOrDefault(s => s.ContentId == navigateToUri.AbsolutePath);
            if (part != null)
            {
                model.SelectedPart = part;
            }
        }
    }

    private void CopyLinkToClipboard(string linkUrl)
    {
        try
        {
            Clipboard.SetText(linkUrl);
            _logger.Information("Copied link to clipboard: {Url}", linkUrl);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to copy link to clipboard: {Url}", linkUrl);
        }
    }
}