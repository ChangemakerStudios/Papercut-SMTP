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


using Papercut.Core.Domain.Message;
using Papercut.Message;
using Papercut.Message.Helpers;

namespace Papercut.ViewModels;

public class MessageDetailViewModel : Conductor<IMessageDetailItem>.Collection.OneActive
{
    readonly IMimeMessageLoader _mimeMessageLoader;
    readonly IMessageRepository _messageRepository;

    int _attachmentCount;

    string? _bcc;

    string? _cc;

    string? _date;

    string? _from;

    string? _htmlFile;

    bool _isHtml;

    bool _isLoading;

    IDisposable? _loadingDisposable;

    private string? _priority;

    private string? _priorityColor;

    int _selectedTabIndex;

    string? _subject;

    string? _textBody;

    string? _to;

    public MessageDetailViewModel(
        Func<MessageDetailPartsListViewModel> partsListViewModelFactory,
        Func<MessageDetailHtmlViewModel> htmlViewModelFactory,
        Func<MessageDetailRawViewModel> rawViewModelFactory,
        Func<MessageDetailHeaderViewModel> headerViewModelFactory,
        Func<MessageDetailBodyViewModel> bodyViewModelFactory,
        IMimeMessageLoader mimeMessageLoader,
        IMessageRepository messageRepository)
    {
        this._mimeMessageLoader = mimeMessageLoader;
        this._messageRepository = messageRepository;

        this.PartsListViewModel = partsListViewModelFactory();
        this.HtmlViewModel = htmlViewModelFactory();
        this.RawViewModel = rawViewModelFactory();
        this.HeaderViewModel = headerViewModelFactory();
        this.BodyViewModel = bodyViewModelFactory();

        this.Items.Add(this.HtmlViewModel);
        this.Items.Add(this.HeaderViewModel);
        this.Items.Add(this.BodyViewModel);
        this.Items.Add(this.PartsListViewModel);
        this.Items.Add(this.RawViewModel);
    }

    public string? Subject
    {
        get => this._subject;
        set
        {
            this._subject = value;
            this.NotifyOfPropertyChange(() => this.Subject);
            this.NotifyOfPropertyChange(() => this.HasMessage);
        }
    }

    public string? To
    {
        get => this._to;
        set
        {
            this._to = value;
            this.NotifyOfPropertyChange(() => this.To);
        }
    }

    public string? Bcc
    {
        get => this._bcc;
        set
        {
            this._bcc = value;
            this.NotifyOfPropertyChange(() => this.Bcc);
        }
    }

    public string? Priority
    {
        get => this._priority;
        set
        {
            this._priority = value;
            this.NotifyOfPropertyChange(() => this.Priority);
        }
    }

    public string? PriorityColor
    {
        get => this._priorityColor;
        set
        {
            this._priorityColor = value;
            this.NotifyOfPropertyChange(() => this.PriorityColor);
        }
    }

    public string? Date
    {
        get => this._date;
        set
        {
            this._date = value;
            this.NotifyOfPropertyChange(() => this.Date);
        }
    }

    public string? From
    {
        get => this._from;
        set
        {
            this._from = value;
            this.NotifyOfPropertyChange(() => this.From);
            this.NotifyOfPropertyChange(() => this.HasMessage);
        }
    }

    public string? CC
    {
        get => this._cc;
        set
        {
            this._cc = value;
            this.NotifyOfPropertyChange(() => this.CC);
        }
    }

    public string? TextBody
    {
        get => this._textBody;
        set
        {
            this._textBody = value;
            this.NotifyOfPropertyChange(() => this.TextBody);
        }
    }

    public bool IsLoading
    {
        get => this._isLoading;
        set
        {
            this._isLoading = value;
            this.NotifyOfPropertyChange(() => this.IsLoading);
        }
    }

    public bool IsHtml
    {
        get => this._isHtml;
        set
        {
            this._isHtml = value;
            this.NotifyOfPropertyChange(() => this.IsHtml);
        }
    }

    public int SelectedTabIndex
    {
        get => this._selectedTabIndex;
        set
        {
            this._selectedTabIndex = value;
            this.NotifyOfPropertyChange(() => this.SelectedTabIndex);
        }
    }

    public int AttachmentCount
    {
        get => this._attachmentCount;
        set
        {
            this._attachmentCount = value;
            this.NotifyOfPropertyChange(() => this.AttachmentCount);
            this.NotifyOfPropertyChange(() => this.HasAttachments);
        }
    }

    public bool HasAttachments => this.AttachmentCount > 0;

    public bool HasMessage => !string.IsNullOrEmpty(this.From) || !string.IsNullOrEmpty(this.Subject);

    public bool HasAnyMessages => this._messageRepository.LoadMessages().Any();

    public string? HtmlFile
    {
        get => this._htmlFile;
        set
        {
            this._htmlFile = value;
            this.NotifyOfPropertyChange(() => this.HtmlFile);
        }
    }

    public MessageDetailPartsListViewModel PartsListViewModel { get; }

    public MessageDetailHtmlViewModel HtmlViewModel { get; }

    public MessageDetailRawViewModel RawViewModel { get; }

    public MessageDetailHeaderViewModel HeaderViewModel { get; }

    public MessageDetailBodyViewModel BodyViewModel { get; }

    public void LoadMessageEntry(MessageEntry? messageEntry)
    {
        this._loadingDisposable?.Dispose();

        var handleLoading = !this.IsLoading;

        if (messageEntry == null)
        {
            // show empty...
            this.DisplayMimeMessage(null);
            if (handleLoading) this.IsLoading = false;
        }
        else
        {
            if (handleLoading) this.IsLoading = true;

            // load and show it...
            this._loadingDisposable = this._mimeMessageLoader.GetObservable(messageEntry).ObserveOn(Dispatcher.CurrentDispatcher).Subscribe(m =>
                {
                    this.DisplayMimeMessage(m);
                    if (handleLoading) this.IsLoading = false;
                },
                e =>
                {
                    var failureMessage =
                        MimeMessage.CreateFromMailMessage(MailMessageHelper.CreateFailureMailMessage(e.Message));

                    this.DisplayMimeMessage(failureMessage);
                    if (handleLoading) this.IsLoading = false;
                });
        }
    }

    void DisplayMimeMessage(MimeMessage? mailMessageEx)
    {
        (string? Name, string Color) GetPriorityText(MimeMessage? message)
        {
            ArgumentNullException.ThrowIfNull(message);

            switch (message.Priority)
            {
                case MessagePriority.NonUrgent: return ("Low", "Blue");
                case MessagePriority.Urgent: return ("High", "Red");
                case MessagePriority.Normal:
                    break;
            }

            return default;
        }

        this.ResetMessage();

        if (mailMessageEx != null)
        {
            this.HeaderViewModel.Headers = string.Join("\r\n", mailMessageEx.Headers.Select(h => h.ToString()));

            var parts = mailMessageEx.BodyParts.OfType<MimePart>().ToList();
            var mainBody = parts.GetMainBodyTextPart();

            this.From = mailMessageEx.From?.ToString() ?? string.Empty;
            this.To = mailMessageEx.To?.ToString() ?? string.Empty;
            this.CC = mailMessageEx.Cc?.ToString() ?? string.Empty;
            this.Bcc = mailMessageEx.Bcc?.ToString() ?? string.Empty;
            var priority = GetPriorityText(mailMessageEx);
            this.Priority = priority.Name;
            this.PriorityColor = priority.Color ?? "Black";
            this.Date = mailMessageEx.Date.ToString();
            this.Subject = mailMessageEx.Subject ?? string.Empty;

            this.AttachmentCount = parts.GetAttachments().Count();

            this.RawViewModel.MimeMessage = mailMessageEx;
            this.PartsListViewModel.MimeMessage = mailMessageEx;

            this.BodyViewModel.Body = mainBody != null ? mainBody.GetText(Encoding.UTF8) : string.Empty;

            if (mainBody != null) {
                this.IsHtml = mainBody.IsContentHtml();
                this.HtmlViewModel.ShowMessage(mailMessageEx);

                if (this.IsHtml)
                {
                    var textPartNotHtml = parts.OfType<TextPart>().Except(new[] { mainBody }).FirstOrDefault();
                    if (textPartNotHtml != null) this.TextBody = textPartNotHtml.GetText(Encoding.UTF8);
                }
            }
        }

        this.SelectedTabIndex = 0;
    }

    void ResetMessage()
    {
        this.AttachmentCount = 0;
        this.IsHtml = false;
        this.HtmlFile = null;
        this.TextBody = null;

        this.HtmlViewModel.HtmlFile = null;
        this.HeaderViewModel.Headers = null;
        this.BodyViewModel.Body = null;
        this.PartsListViewModel.MimeMessage = null;

        this.NotifyOfPropertyChange(() => this.HasAnyMessages);
    }
}