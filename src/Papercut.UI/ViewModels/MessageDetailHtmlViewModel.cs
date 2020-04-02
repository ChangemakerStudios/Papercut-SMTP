// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Navigation;

    using Caliburn.Micro;

    using MimeKit;

    using Papercut.Common.Extensions;
    using Papercut.Core.Annotations;
    using Papercut.Helpers;
    using Papercut.Services;
    using Papercut.Views;

    using Serilog;

    using Action = Caliburn.Micro.Action;

    public class MessageDetailHtmlViewModel : Screen, IMessageDetailItem
    {
        string _htmlFile;

        readonly ILogger _logger;

        readonly IHtmlPreviewGenerator _previewGenerator;

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

        public Uri HtmlFileUri => HtmlFile != null ? new Uri(HtmlFile) : null;

        public bool HasHtmlFile => !string.IsNullOrWhiteSpace(HtmlFile);

        [DllImport("urlmon.dll")]
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Error)]
        static extern int CoInternetSetFeatureEnabled(
            int featureEntry,
            [MarshalAs(UnmanagedType.U4)] int dwFlags,
            bool fEnable);

        public void ShowMessage([NotNull] MimeMessage mailMessageEx)
        {
            if (mailMessageEx == null)
                throw new ArgumentNullException(nameof(mailMessageEx));

            try
            {
                HtmlFile = _previewGenerator.CreateFile(mailMessageEx);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failure Saving Browser Temp File for {MailMessage}", mailMessageEx.ToString());
            }
        }

        [UsedImplicitly]
        public void OnNavigating(NavigatingCancelEventArgs e)
        {
            e.Cancel = this.TryHandleNavigateToUri(e.Uri);
        }

        private bool TryHandleNavigateToUri([NotNull] Uri navigateToUri)
        {
            if (navigateToUri == null) throw new ArgumentNullException(nameof(navigateToUri));

            if (navigateToUri.Equals(HtmlFileUri))
            {
                return false;
            }

            if (navigateToUri.Scheme == Uri.UriSchemeHttp || navigateToUri.Scheme == Uri.UriSchemeHttps)
            {
                Process.Start(navigateToUri.AbsoluteUri);
            }
            else if (navigateToUri.Scheme.Equals("cid", StringComparison.OrdinalIgnoreCase))
            {
                // direct to the parts area...
                var model = this.GetConductor().ActivateViewModelOf<MessageDetailPartsListViewModel>();
                var part = model.Parts.FirstOrDefault(s => s.ContentId == navigateToUri.AbsolutePath);
                if (part != null)
                {
                    model.SelectedPart = part;
                }
            }

            // always cancel
            return true;
        }

        protected override void OnViewLoaded(object view)
        {
            const int Feature = 21; //FEATURE_DISABLE_NAVIGATION_SOUNDS
            const int SetFeatureOnProcess = 0x00000002;

            base.OnViewLoaded(view);

            if (!(view is MessageDetailHtmlView typedView))
            {
                _logger.Error("Unable to locate the MessageDetailHtmlView to hook the WebBrowser Control");
                return;
            }

            try
            {
                // disable the stupid click sound on navigate
                var enabled = CoInternetSetFeatureEnabled(Feature, SetFeatureOnProcess, true);
            }
            catch (Exception ex)
            {
                // just have to live with the sound
                _logger.Warning(ex, "Failed to disable the Navigation Sounds on the WebBrowser control");
            }

            this.GetPropertyValues(p => p.HtmlFile)
                .Subscribe(
                    file =>
                    {
                        typedView.htmlView.Source = new Uri(string.IsNullOrWhiteSpace(file) ? "about:blank" : file);
                    });

            void VisibilityChanged(DependencyPropertyChangedEventArgs o)
            {
                typedView.htmlView.Visibility = o.NewValue.ToType<bool>()
                                                    ? Visibility.Visible
                                                    : Visibility.Collapsed;
            }

            Observable.FromEvent<DependencyPropertyChangedEventHandler, DependencyPropertyChangedEventArgs>(
                a => ((s, e) => a(e)),
                h => typedView.IsEnabledChanged += h,
                h => typedView.IsEnabledChanged -= h)
                .Throttle(TimeSpan.FromMilliseconds(100))
                .ObserveOnDispatcher()
                .Subscribe(VisibilityChanged);

            typedView.htmlView.ContextMenuOpening += (sender, args) =>
            {
                args.Handled = true;
            };

            if (!BrowserHandler.TryGetWebBrowserInstance(typedView.htmlView, out var wb))
            {
                this._logger.Warning("Failure Retrieving COM+ Browser Instance");
            }

            if (wb != null)
            {
                wb.NewWindow3 += (ref object disp, ref bool cancel, uint flags, string context, string url) =>
                {
                    cancel = TryHandleNavigateToUri(new Uri(url));
                };
            }

            typedView.htmlView.Navigated += (sender, args) =>
            {
                if (wb != null)
                {
                    wb.Silent = true;
                }
            };
        }

        /// <summary>
        /// From SO: http://stackoverflow.com/questions/1298255/how-do-i-suppress-script-errors-when-using-the-wpf-webbrowser-control
        /// </summary>
        static class BrowserHandler
        {
            private static Guid IWebBrowserAppGUID = new Guid("0002DF05-0000-0000-C000-000000000046");
            private static Guid IWebBrowser2GUID = typeof(SHDocVw.WebBrowser).GUID;

            internal static bool TryGetWebBrowserInstance(WebBrowser browser, out SHDocVw.WebBrowser webBrowser)
            {
                webBrowser = null;

                // get an IWebBrowser from the document
                if (!(browser?.Document is IServiceProvider serviceProvider)) return false;

                webBrowser = (SHDocVw.WebBrowser)serviceProvider.QueryService(ref IWebBrowserAppGUID, ref IWebBrowser2GUID);

                if (webBrowser == null) return false;

                return true;
            }
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("6d5140c1-7436-11ce-8034-00aa006009fa")]
        internal interface IServiceProvider
        {
            [return: MarshalAs(UnmanagedType.IUnknown)]
            object QueryService(ref Guid guidService, ref Guid riid);
        }

        [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleServiceProvider
        {
            [PreserveSig]
            int QueryService([In] ref Guid guidService, [In] ref Guid riid, [MarshalAs(UnmanagedType.IDispatch)] out object ppvObject);
        }
    }
}