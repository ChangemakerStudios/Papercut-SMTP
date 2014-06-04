// /*  
//  * Papercut
//  *
//  *  Copyright © 2008 - 2012 Ken Robertson
//  *  Copyright © 2013 - 2014 Jaben Cargman
//  *  
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *  
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *  
//  *  Unless required by applicable law or agreed to in writing, software
//  *  distributed under the License is distributed on an "AS IS" BASIS,
//  *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *  See the License for the specific language governing permissions and
//  *  limitations under the License.
//  *  
//  */


namespace Papercut.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Mime;
    using System.Reactive.Linq;
    using System.Windows;
    using System.Windows.Navigation;

    using Caliburn.Micro;

    using MimeKit;

    using Papercut.Core.Helper;
    using Papercut.Helpers;
    using Papercut.Views;

    using Serilog;

    public class MessageDetailViewModel : Screen
    {
        string _subject;

        string _to;

        string _bcc;

        string _date;

        string _from;

        string _cc;

        string _headers;

        string _textBody;

        string _body;

        bool _isHtml;

        string _htmlFile;

        int _selectedTabIndex;

        void SetBrowserDocument(MimeMessage mailMessageEx)
        {
            Observable.Start(
                () =>
                {
                    try
                    {
                        return mailMessageEx.CreateHtmlPreviewFile();
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(
                            ex,
                            "Exception Saving Browser Temp File for {MailMessage}",
                            mailMessageEx.ToString());
                    }

                    return null;
                }).Where(s => !string.IsNullOrEmpty(s)).Subscribe(
                    h =>
                    {
                        HtmlFile = h;
                    });
        }

        public string Subject
        {
            get
            {
                return _subject;
            }
            set
            {
                _subject = value;
                NotifyOfPropertyChange(() => Subject);
            }
        }

        public string To
        {
            get
            {
                return _to;
            }
            set
            {
                _to = value;
                NotifyOfPropertyChange(() => To);
            }
        }

        public string Bcc
        {
            get
            {
                return _bcc;
            }
            set
            {
                _bcc = value;
                NotifyOfPropertyChange(() => Bcc);
            }
        }

        public string Date
        {
            get
            {
                return _date;
            }
            set
            {
                _date = value;
                NotifyOfPropertyChange(() => Date);
            }
        }

        public string From
        {
            get
            {
                return _from;
            }
            set
            {
                _from = value;
                NotifyOfPropertyChange(() => From);
            }
        }

        public string CC
        {
            get
            {
                return _cc;
            }
            set
            {
                _cc = value;
                NotifyOfPropertyChange(() => CC);
            }
        }

        public string Headers
        {
            get
            {
                return _headers;
            }
            set
            {
                _headers = value;
                NotifyOfPropertyChange(() => Headers);
            }
        }

        public string TextBody
        {
            get
            {
                return _textBody;
            }
            set
            {
                _textBody = value;
                NotifyOfPropertyChange(() => TextBody);
            }
        }

        public string Body
        {
            get
            {
                return _body;
            }
            set
            {
                _body = value;
                NotifyOfPropertyChange(() => Body);
            }
        }

        public bool IsHtml
        {
            get
            {
                return _isHtml;
            }
            set
            {
                _isHtml = value;
                NotifyOfPropertyChange(() => IsHtml);
            }
        }

        public int SelectedTabIndex
        {
            get
            {
                return _selectedTabIndex;
            }
            set
            {
                _selectedTabIndex = value;
                NotifyOfPropertyChange(() => SelectedTabIndex);
            }
        }

        public string HtmlFile
        {
            get
            {
                return _htmlFile;
            }
            set
            {
                _htmlFile = value;
                NotifyOfPropertyChange(() => HtmlFile);
            }
        }

        public void DisplayMimeMessage(MimeMessage mailMessageEx)
        {
            Headers = string.Join("\r\n", mailMessageEx.Headers.Select(h => h.ToString()));

            List<MimePart> parts = mailMessageEx.BodyParts.ToList();
            TextPart mainBody = parts.GetMainBodyTextPart();
            Body = mainBody.Text;

            From = mailMessageEx.From.IfNotNull(s => s.ToString()) ?? string.Empty;
            To = mailMessageEx.To.IfNotNull(s => s.ToString()) ?? string.Empty;
            CC = mailMessageEx.Cc.IfNotNull(s => s.ToString()) ?? string.Empty;
            Bcc = mailMessageEx.Bcc.IfNotNull(s => s.ToString()) ?? string.Empty;
            Date = mailMessageEx.Date.IfNotNull(s => s.ToString()) ?? string.Empty;

            Subject = mailMessageEx.Subject ?? string.Empty;

            //SetWindowTitle(subject);

            IsHtml = mainBody.IsContentHtml();
            HtmlFile = null;
            TextBody = null;

            SetBrowserDocument(mailMessageEx);

            if (IsHtml)
            {
                TextPart textPartNotHtml =
                    parts.OfType<TextPart>().Except(new[] { mainBody }).FirstOrDefault();

                if (textPartNotHtml != null)
                {
                    TextBody = textPartNotHtml.Text;
                }
            }

            this.SelectedTabIndex = 0;

            //if (defaultTab.IsVisible) tabControl.SelectedIndex = 0;

            //defaultHtmlView.Visibility = isContentHtml ? Visibility.Visible : Visibility.Collapsed;
            //defaultBodyView.Visibility = isContentHtml ? Visibility.Collapsed : Visibility.Visible;

            //SpinAnimation.Visibility = Visibility.Collapsed;
            //tabControl.IsEnabled = true;

            //// Enable the delete and forward button
            //DeleteSelected.IsEnabled = true;
            //ForwardSelected.IsEnabled = true;
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            var typedView = view as MessageDetailView;

            if (typedView != null)
            {
                typedView.Loaded += (sender, args) =>
                {
                    typedView.defaultHtmlView.Content = null;
                    typedView.defaultHtmlView.RemoveBackEntry();
                };

                this.GetPropertyValues(p => p.HtmlFile)
                    .ObserveOnDispatcher()
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Subscribe(
                        (url) =>
                        {
                            typedView.defaultHtmlView.Navigate(new Uri(url));
                            typedView.defaultHtmlView.Refresh();
                        });
            }
        }
    }
}