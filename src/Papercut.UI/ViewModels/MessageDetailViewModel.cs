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
    using System.Linq;
    using System.Reactive.Linq;
    using System.Text;

    using Caliburn.Micro;

    using MimeKit;

    using Papercut.Common.Extensions;
    using Papercut.Core.Domain.Message;
    using Papercut.Helpers;
    using Papercut.Message;
    using Papercut.Message.Helpers;

    public class MessageDetailViewModel : Conductor<IMessageDetailItem>.Collection.OneActive
    {
        int _attachmentCount;

        string _bcc;

        string _cc;

        string _date;

        string _from;

        string _htmlFile;

        bool _isHtml;

        bool _isLoading;

        IDisposable _loadingDisposable;

        int _selectedTabIndex;

        string _subject;

        string _textBody;

        string _to;

        readonly MimeMessageLoader _mimeMessageLoader;

        public MessageDetailViewModel(
            Func<MessageDetailPartsListViewModel> partsListViewModelFactory,
            Func<MessageDetailHtmlViewModel> htmlViewModelFactory,
            Func<MessageDetailRawViewModel> rawViewModelFactory,
            Func<MessageDetailHeaderViewModel> headerViewModelFactory,
            Func<MessageDetailBodyViewModel> bodyViewModelFactory,
            MimeMessageLoader mimeMessageLoader)
        {
            _mimeMessageLoader = mimeMessageLoader;

            PartsListViewModel = partsListViewModelFactory();
            HtmlViewModel = htmlViewModelFactory();
            RawViewModel = rawViewModelFactory();
            HeaderViewModel = headerViewModelFactory();
            BodyViewModel = bodyViewModelFactory();

            Items.Add(HtmlViewModel);
            Items.Add(HeaderViewModel);
            Items.Add(BodyViewModel);
            Items.Add(PartsListViewModel);
            Items.Add(RawViewModel);
        }

        public string Subject
        {
            get { return _subject; }
            set
            {
                _subject = value;
                NotifyOfPropertyChange(() => Subject);
            }
        }

        public string To
        {
            get { return _to; }
            set
            {
                _to = value;
                NotifyOfPropertyChange(() => To);
            }
        }

        public string Bcc
        {
            get { return _bcc; }
            set
            {
                _bcc = value;
                NotifyOfPropertyChange(() => Bcc);
            }
        }

        public string Date
        {
            get { return _date; }
            set
            {
                _date = value;
                NotifyOfPropertyChange(() => Date);
            }
        }

        public string From
        {
            get { return _from; }
            set
            {
                _from = value;
                NotifyOfPropertyChange(() => From);
            }
        }

        public string CC
        {
            get { return _cc; }
            set
            {
                _cc = value;
                NotifyOfPropertyChange(() => CC);
            }
        }

        public string TextBody
        {
            get { return _textBody; }
            set
            {
                _textBody = value;
                NotifyOfPropertyChange(() => TextBody);
            }
        }

        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                NotifyOfPropertyChange(() => IsLoading);
            }
        }

        public bool IsHtml
        {
            get { return _isHtml; }
            set
            {
                _isHtml = value;
                NotifyOfPropertyChange(() => IsHtml);
            }
        }

        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set
            {
                _selectedTabIndex = value;
                NotifyOfPropertyChange(() => SelectedTabIndex);
            }
        }

        public int AttachmentCount
        {
            get { return _attachmentCount; }
            set
            {
                _attachmentCount = value;
                NotifyOfPropertyChange(() => AttachmentCount);
                NotifyOfPropertyChange(() => HasAttachments);
            }
        }

        public bool HasAttachments => AttachmentCount > 0;

        public string HtmlFile
        {
            get { return _htmlFile; }
            set
            {
                _htmlFile = value;
                NotifyOfPropertyChange(() => HtmlFile);
            }
        }

        public MessageDetailPartsListViewModel PartsListViewModel { get; private set; }

        public MessageDetailHtmlViewModel HtmlViewModel { get; private set; }

        public MessageDetailRawViewModel RawViewModel { get; private set; }

        public MessageDetailHeaderViewModel HeaderViewModel { get; private set; }

        public MessageDetailBodyViewModel BodyViewModel { get; private set; }

        public void LoadMessageEntry(MessageEntry messageEntry)
        {
            _loadingDisposable?.Dispose();

            var handleLoading = !IsLoading;

            if (messageEntry == null)
            {
                // show empty...
                DisplayMimeMessage(null);
                if (handleLoading)
                    IsLoading = false;
            }
            else
            {
                if (handleLoading)
                    IsLoading = true;

                // load and show it...
                _loadingDisposable = _mimeMessageLoader.Get(messageEntry).ObserveOnDispatcher().Subscribe(m =>
                {
                    DisplayMimeMessage(m);
                    if (handleLoading)
                        IsLoading = false;
                },
                    e =>
                    {
                        var failureMessage =
                            MimeMessage.CreateFromMailMessage(MailMessageHelper.CreateFailureMailMessage(e.Message));

                        DisplayMimeMessage(failureMessage);
                        if (handleLoading)
                            IsLoading = false;
                    });
            }
        }

        void DisplayMimeMessage(MimeMessage mailMessageEx)
        {
            ResetMessage();

            if (mailMessageEx != null)
            {
                HeaderViewModel.Headers = string.Join("\r\n", mailMessageEx.Headers.Select(h => h.ToString()));

                var parts = mailMessageEx.BodyParts.OfType<MimePart>().ToList();
                var mainBody = parts.GetMainBodyTextPart();

                From = mailMessageEx.From?.ToString() ?? string.Empty;
                To = mailMessageEx.To?.ToString() ?? string.Empty;
                CC = mailMessageEx.Cc?.ToString() ?? string.Empty;
                Bcc = mailMessageEx.Bcc?.ToString() ?? string.Empty;
                Date = mailMessageEx.Date.ToString();
                Subject = mailMessageEx.Subject ?? string.Empty;
                
                AttachmentCount = parts.GetAttachments().Count();
                RawViewModel.MimeMessage = mailMessageEx;
                PartsListViewModel.MimeMessage = mailMessageEx;

                BodyViewModel.Body = mainBody != null ? mainBody.GetText(Encoding.UTF8) : string.Empty;

                if (mainBody != null) {
                    IsHtml = mainBody.IsContentHtml();
                    HtmlViewModel.ShowMessage(mailMessageEx);

                    if (IsHtml)
                    {
                        var textPartNotHtml = parts.OfType<TextPart>().Except(new[] { mainBody }).FirstOrDefault();
                        if (textPartNotHtml != null)
                            TextBody = textPartNotHtml.GetText(Encoding.UTF8);
                    }
                }
            }

            SelectedTabIndex = 0;
        }

        void ResetMessage()
        {
            AttachmentCount = 0;
            IsHtml = false;
            HtmlFile = null;
            TextBody = null;

            HtmlViewModel.HtmlFile = null;
            HeaderViewModel.Headers = null;
            BodyViewModel.Body = null;
            PartsListViewModel.MimeMessage = null;
        }
    }
}