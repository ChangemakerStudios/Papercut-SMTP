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


using Microsoft.Web.WebView2.Core;

using Papercut.AppLayer.Uris;
using Papercut.Core.Infrastructure.Async;
using Papercut.Core.Infrastructure.Logging;
using Papercut.Domain.HtmlPreviews;
using Papercut.Infrastructure.WebView;
using Papercut.Views;

namespace Papercut.ViewModels;

public class MessageDetailHtmlViewModel : Screen, IMessageDetailItem
{
    readonly ILogger _logger;

    readonly IHtmlPreviewGenerator _previewGenerator;

    private readonly WebView2Information _webView2Information;

    private string? _htmlFile;

    private bool _isWebViewInstalled = false;

    public MessageDetailHtmlViewModel(ILogger logger, WebView2Information webView2Information, IHtmlPreviewGenerator previewGenerator)
    {
        this.DisplayName = "Message";
        this._logger = logger;
        this._webView2Information = webView2Information;
        this._previewGenerator = previewGenerator;
        this.IsWebViewInstalled = this._webView2Information.IsInstalled;
    }

    public bool IsWebViewInstalled
    {

        get => this._isWebViewInstalled;

        set
        {
            this._isWebViewInstalled = value;
            this.NotifyOfPropertyChange(() => this.IsWebViewInstalled);
            this.NotifyOfPropertyChange(() => this.ShowHtmlView);
        }
    }

    public string? HtmlFile
    {

        get => this._htmlFile;

        set
        {
            this._htmlFile = value;
            this.NotifyOfPropertyChange(() => this.HtmlFile);
            this.NotifyOfPropertyChange(() => this.ShowHtmlView);
        }
    }

    public bool ShowHtmlView => !string.IsNullOrWhiteSpace(this.HtmlFile);

    public void ShowMessage(MimeMessage? mailMessageEx)
    {
        ArgumentNullException.ThrowIfNull(mailMessageEx);

        try
        {
            this.HtmlFile = this._previewGenerator.GetHtmlPreviewFile(mailMessageEx);
        }
        catch (Exception ex)
        {
            this._logger.Error(ex, "Failure Saving Browser Temp File for {MailMessage}", mailMessageEx.ToString());
        }
    }

    private bool IsLocalNavigation(string navigateToUrl)
    {
        if (string.IsNullOrEmpty(navigateToUrl))
        {
            return true;
        }

        if (navigateToUrl.StartsWith("file:") || navigateToUrl.StartsWith("about:") || navigateToUrl.StartsWith("data:text/html"))
        {
            return true;
        }

        return false;
    }

    protected override void OnViewLoaded(object view)
    {
        base.OnViewLoaded(view);

        if (view is not MessageDetailHtmlView typedView)
        {
            this._logger.Error("Unable to locate the MessageDetailHtmlView to hook the WebBrowser Control");
            return;
        }

        typedView.htmlView.CoreWebView2InitializationCompleted += (_, args) =>
        {
            if (!args.IsSuccess)
            {
                this._logger.Error(
                    args.InitializationException,
                    "Failure Initializing Edge WebView2");

            }
            else
            {
                this.SetupWebView(typedView.htmlView.CoreWebView2);
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

        typedView.htmlView.ContextMenuOpening += (_, args) =>
        {
            args.Handled = true;
        };
    }

    private void SetupWebView(CoreWebView2 coreWebView)
    {
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
            var internalUrl = !args.IsUserInitiated || this.IsLocalNavigation(args.Uri);

            if (internalUrl)
            {
                args.Handled = false;
                return;
            }

            // external navigation
            args.Handled = true;

            try
            {
                await this.DoInternalNavigationAsync(new Uri(args.Uri));
            }
            catch (Exception ex) when (_logger.ErrorWithContext(ex, "Failure Navigating to External Url {Url}", args.Uri))
            {
            }
        };

        coreWebView.NavigationStarting += async (_, args) =>
        {
            var uri = args.Uri;

            var internalUrl = !args.IsUserInitiated || this.IsLocalNavigation(uri);

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
                await this.DoInternalNavigationAsync(new Uri(uri));
            }
            catch (Exception ex) when (_logger.ErrorWithContext(ex, "Failure Navigating to External Url {Url}", uri))
            {
            }
        };

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

    private async Task DoInternalNavigationAsync(Uri navigateToUri)
    {
        if (navigateToUri.Scheme == Uri.UriSchemeHttp || navigateToUri.Scheme == Uri.UriSchemeHttps)
        {
            navigateToUri.OpenUri();
        }
        else if (navigateToUri.Scheme.Equals("cid", StringComparison.OrdinalIgnoreCase))
        {
            // direct to the parts area...
            var model = await this.GetConductor().ActivateViewModelOf<MessageDetailPartsListViewModel>();
            var part = model.Parts.FirstOrDefault(s => s.ContentId == navigateToUri.AbsolutePath);
            if (part != null)
            {
                model.SelectedPart = part;
            }
        }
    }
}