// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2021 Jaben Cargman
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


namespace Papercut.ViewModels
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Navigation;

    using Caliburn.Micro;

    using Microsoft.Web.WebView2.Core;

    using MimeKit;

    using Papercut.Common.Extensions;
    using Papercut.Core.Annotations;
    using Papercut.Domain.HtmlPreviews;
    using Papercut.Helpers;
    using Papercut.Views;

    using Serilog;

    using Action = Caliburn.Micro.Action;
    public class MessageDetailHtmlViewModel : Screen, IMessageDetailItem
    {
        readonly ILogger _logger;

        readonly IHtmlPreviewGenerator _previewGenerator;

        private string _htmlPreview;

        public MessageDetailHtmlViewModel(ILogger logger, IHtmlPreviewGenerator previewGenerator)
        {
            DisplayName = "Message";
            _logger = logger;
            _previewGenerator = previewGenerator;
        }

        public string HtmlFile
        {

            get => _htmlFile;

            set
            {
                _htmlFile = value;
                NotifyOfPropertyChange(() => HtmlFile);
                NotifyOfPropertyChange(() => HasHtmlFile);
            }
        }

        public bool HasHtmlPreview => !string.IsNullOrWhiteSpace(this.HtmlPreview);

        public void ShowMessage([NotNull] MimeMessage mailMessageEx)
        {
            if (mailMessageEx == null)
                throw new ArgumentNullException(nameof(mailMessageEx));

            try
            {
                this.HtmlPreview = this._previewGenerator.GetHtmlPreview(mailMessageEx);
            }
            catch (Exception ex)
            {
                this._logger.Error(ex, "Failure Saving Browser Temp File for {MailMessage}", mailMessageEx.ToString());
            }
        }

        private bool ShouldNavigateToUrl([NotNull] string navigateToUrl)
        {
            if (string.IsNullOrEmpty(navigateToUrl))
            {
                return true;
            }

            if (navigateToUrl.StartsWith("about:") || navigateToUrl.StartsWith("data:text/html"))
            {
                return true;
            }

            var navigateToUri = new Uri(navigateToUrl);

            if (navigateToUri.Scheme == Uri.UriSchemeHttp || navigateToUri.Scheme == Uri.UriSchemeHttps)
            {
                Process.Start(navigateToUri.AbsoluteUri);
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

            return false;
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            if (!(view is MessageDetailHtmlView typedView))
            {
                this._logger.Error("Unable to locate the MessageDetailHtmlView to hook the WebBrowser Control");
                return;
            }

            typedView.htmlView.CoreWebView2InitializationCompleted += (sender, args) =>
            {
                this.SetupWebView(typedView.htmlView.CoreWebView2);
            };

            void VisibilityChanged(DependencyPropertyChangedEventArgs o)
            {
                typedView.htmlView.Visibility = o.NewValue.ToType<bool>()
                                                    ? Visibility.Visible
                                                    : Visibility.Collapsed;
            }

            Observable.FromEvent<DependencyPropertyChangedEventHandler, DependencyPropertyChangedEventArgs>(
                a => (s, e) => a(e),
                h => typedView.IsEnabledChanged += h,
                h => typedView.IsEnabledChanged -= h)
                .Throttle(TimeSpan.FromMilliseconds(100))
                .ObserveOnDispatcher()
                .Subscribe(VisibilityChanged);

            typedView.htmlView.ContextMenuOpening += (sender, args) =>
            {
                args.Handled = true;
            };
        }

        private void SetupWebView(CoreWebView2 coreWebView)
        {
            coreWebView.NavigationStarting += (sender, e) =>
            {
                e.Cancel = !this.ShouldNavigateToUrl(e.Uri);
            };

            coreWebView.DisableEdgeFeatures();

            this.GetPropertyValues(p => p.HtmlPreview)
                .Subscribe(
                    n =>
                    {
                        coreWebView.NavigateToString(n ?? string.Empty);
                    }
                );
        }
    }
}