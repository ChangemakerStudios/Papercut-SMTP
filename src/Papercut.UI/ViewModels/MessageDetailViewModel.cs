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
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Text;

    using Caliburn.Micro;

    using Core.Annotations;
    using Core.Domain.Message;

    using Helpers;

    using Message;
    using Message.Helpers;

    using MimeKit;

    public class MessageDetailViewModel : Conductor<IMessageDetailItem>.Collection.OneActive
    {
        readonly MimeMessageLoader _mimeMessageLoader;

        int _attachmentCount;

        string _bcc;

        string _cc;

        string _date;

        string _from;

        string _htmlFile;

        bool _isHtml;

        bool _isLoading;

        IDisposable _loadingDisposable;
        private string _priority;

        int _selectedTabIndex;

        string _subject;

        string _textBody;

        string _to;
        private string _priorityColor;

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
            get => _subject;
            set
            {
                _subject = value;
                NotifyOfPropertyChange(() => Subject);
            }
        }

        public string To
        {
            get => _to;
            set
            {
                _to = value;
                NotifyOfPropertyChange(() => To);
            }
        }

        public string Bcc
        {
            get => _bcc;
            set
            {
                _bcc = value;
                NotifyOfPropertyChange(() => Bcc);
            }
        }

        public string Priority
        {
            get => _priority;
            set
            {
                _priority = value;
                NotifyOfPropertyChange(() => Priority);
            }
        }

        public string PriorityColor
        {
            get => _priorityColor;
            set
            {
                _priorityColor = value;
                NotifyOfPropertyChange(() => PriorityColor);
            }
        }

        public string Date
        {
            get => _date;
            set
            {
                _date = value;
                NotifyOfPropertyChange(() => Date);
            }
        }

        public string From
        {
            get => _from;
            set
            {
                _from = value;
                NotifyOfPropertyChange(() => From);
            }
        }

        public string CC
        {
            get => _cc;
            set
            {
                _cc = value;
                NotifyOfPropertyChange(() => CC);
            }
        }

        public string TextBody
        {
            get => _textBody;
            set
            {
                _textBody = value;
                NotifyOfPropertyChange(() => TextBody);
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                NotifyOfPropertyChange(() => IsLoading);
            }
        }

        public bool IsHtml
        {
            get => _isHtml;
            set
            {
                _isHtml = value;
                NotifyOfPropertyChange(() => IsHtml);
            }
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                _selectedTabIndex = value;
                NotifyOfPropertyChange(() => SelectedTabIndex);
            }
        }

        public int AttachmentCount
        {
            get => _attachmentCount;
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
            get => _htmlFile;
            set
            {
                _htmlFile = value;
                NotifyOfPropertyChange(() => HtmlFile);
            }
        }

        public MessageDetailPartsListViewModel PartsListViewModel { get; }

        public MessageDetailHtmlViewModel HtmlViewModel { get; }

        public MessageDetailRawViewModel RawViewModel { get; }

        public MessageDetailHeaderViewModel HeaderViewModel { get; }

        public MessageDetailBodyViewModel BodyViewModel { get; }

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
                _loadingDisposable = _mimeMessageLoader.GetObservable(messageEntry).ObserveOnDispatcher().Subscribe(m =>
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
                var priority = GetPriority(mailMessageEx);
                Priority = priority.Name;
                PriorityColor = priority.Color;
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

        private static (string Name, string Color) GetPriority([NotNull] MimeMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            switch (message.Priority)
            {
                case MessagePriority.NonUrgent: return ("Low", "Blue");
                case MessagePriority.Urgent: return ("High", "Red");
                case MessagePriority.Normal:
                    break;
            }

            return default;
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