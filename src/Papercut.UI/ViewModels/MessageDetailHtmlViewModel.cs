// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2017 Jaben Cargman
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
    using System.Windows.Navigation;

    using Caliburn.Micro;

    using MimeKit;

    using Papercut.Common.Extensions;
    using Papercut.Core.Annotations;
    using Papercut.Helpers;
    using Papercut.Services;
    using Papercut.Views;

    using Serilog;

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
            get { return _htmlFile; }
            set
            {
                _htmlFile = value;
                NotifyOfPropertyChange(() => HtmlFile);
                NotifyOfPropertyChange(() => HasHtmlFile);
            }
        }

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

            Observable.Start(() =>
            {
                try
                {
                    return _previewGenerator.CreateFile(mailMessageEx);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failure Saving Browser Temp File for {MailMessage}", mailMessageEx.ToString());
                }

                return null;
            }).Subscribe(h => { HtmlFile = h; });
        }

        public void OnNavigating(NavigatingCancelEventArgs e)
        {
            if (e.Uri.Scheme == Uri.UriSchemeHttp || e.Uri.Scheme == Uri.UriSchemeHttps)
            {
                e.Cancel = true;
                Process.Start(e.Uri.AbsoluteUri);
            }
            else if (e.Uri.Scheme.Equals("cid", StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;

                // direct to the parts area...
                var model = this.GetConductor().ActivateViewModelOf<MessageDetailPartsListViewModel>();
                var part = model.Parts.FirstOrDefault(s => s.ContentId == e.Uri.AbsolutePath);
                if (part != null)
                {
                    model.SelectedPart = part;
                }
            }
        }

        protected override void OnViewLoaded(object view)
        {
            const int Feature = 21; //FEATURE_DISABLE_NAVIGATION_SOUNDS
            const int SetFeatureOnProcess = 0x00000002;

            base.OnViewLoaded(view);

            var typedView = view as MessageDetailHtmlView;

            if (typedView == null)
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

            Observable.FromEvent<DependencyPropertyChangedEventHandler, DependencyPropertyChangedEventArgs>(
                a => ((s, e) => a(e)),
                h => typedView.IsEnabledChanged += h,
                h => typedView.IsEnabledChanged -= h)
                .Throttle(TimeSpan.FromMilliseconds(100))
                .ObserveOnDispatcher()
                .Subscribe(
                    o =>
                    {
                        typedView.htmlView.Visibility = o.NewValue.ToType<bool>()
                                                            ? Visibility.Visible
                                                            : Visibility.Collapsed;
                    });

            typedView.htmlView.Navigated += (sender, args) =>
            {
                BrowserHandler.SetSilent(typedView.htmlView, true);
            };
        }

        /// <summary>
        /// From SO: http://stackoverflow.com/questions/1298255/how-do-i-suppress-script-errors-when-using-the-wpf-webbrowser-control
        /// </summary>
        static class BrowserHandler
        {
            private const string IWebBrowserAppGUID = "0002DF05-0000-0000-C000-000000000046";
            private const string IWebBrowser2GUID = "D30C1661-CDAF-11d0-8A3E-00C04FC9E26E";

            internal static void SetSilent(System.Windows.Controls.WebBrowser browser, bool silent)
            {
                // get an IWebBrowser2 from the document
                IOleServiceProvider sp = browser?.Document as IOleServiceProvider;
                if (sp != null)
                {
                    Guid IID_IWebBrowserApp = new Guid(IWebBrowserAppGUID);
                    Guid IID_IWebBrowser2 = new Guid(IWebBrowser2GUID);

                    object webBrowser;
                    sp.QueryService(ref IID_IWebBrowserApp, ref IID_IWebBrowser2, out webBrowser);
                    webBrowser?.GetType()
                        .InvokeMember(
                            "Silent",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.PutDispProperty,
                            null,
                            webBrowser,
                            new object[] { silent });
                }
            }

        }

        [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleServiceProvider
        {
            [PreserveSig]
            int QueryService([In] ref Guid guidService, [In] ref Guid riid, [MarshalAs(UnmanagedType.IDispatch)] out object ppvObject);


        }
    }
}