// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2014 Jaben Cargman
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
    using System.Reactive.Linq;
    using System.Windows;
    using System.Windows.Navigation;

    using Caliburn.Micro;

    using MimeKit;

    using Papercut.Core.Annotations;
    using Papercut.Core.Helper;
    using Papercut.Helpers;
    using Papercut.Views;

    using Serilog;

    public class MessageViewModel : Screen
    {
        readonly ILogger _logger;

        string _htmlFile;

        public MessageViewModel(ILogger logger)
        {
            _logger = logger;
        }

        public string HtmlFile
        {
            get { return _htmlFile; }
            set
            {
                _htmlFile = value;
                NotifyOfPropertyChange(() => HtmlFile);
            }
        }

        public void ShowMessage([NotNull] MimeMessage mailMessageEx)
        {
            if (mailMessageEx == null) throw new ArgumentNullException("mailMessageEx");

            Observable.Start(
                () =>
                {
                    try
                    {
                        return mailMessageEx.CreateHtmlPreviewFile();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(
                            ex,
                            "Exception Saving Browser Temp File for {MailMessage}",
                            mailMessageEx.ToString());
                    }

                    return null;
                }).Where(s => !string.IsNullOrEmpty(s))
                .Subscribe(h => HtmlFile = h);
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            var typedView = view as MessageView;

            if (typedView == null)
            {
                _logger.Error("Unable to locate the MessageView to hook the Frame Control");
                return;
            }

            this.GetPropertyValues(p => p.HtmlFile)
                .ObserveOnDispatcher()
                .Subscribe(
                    file =>
                    {
                        typedView.defaultHtmlView.NavigationUIVisibility =
                            NavigationUIVisibility.Hidden;

                        if (!string.IsNullOrWhiteSpace(file))
                        {
                            typedView.defaultHtmlView.Navigate(new Uri(file));
                            typedView.defaultHtmlView.Refresh();
                        }
                        else typedView.defaultHtmlView.Content = null;
                    });

            Observable
                .FromEvent
                <DependencyPropertyChangedEventHandler, DependencyPropertyChangedEventArgs>(
                    a => ((s, e) => a(e)),
                    h => typedView.IsEnabledChanged += h,
                    h => typedView.IsEnabledChanged -= h)
                .Subscribe(
                    o =>
                    {
                        // not enabled -- set the control to file to null
                        if (!o.NewValue.ToType<bool>()) HtmlFile = null;
                    });
        }
    }
}