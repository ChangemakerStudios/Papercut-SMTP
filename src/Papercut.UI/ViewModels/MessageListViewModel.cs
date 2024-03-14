// Papercut SMTP
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Data;
using System.Windows.Input;

using Caliburn.Micro;

using DynamicData;

using Papercut.Common.Domain;
using Papercut.Core.Domain.Message;
using Papercut.Events;
using Papercut.Helpers;
using Papercut.Message;
using Papercut.Properties;

using Action = System.Action;

namespace Papercut.ViewModels;

using Action = Action;
using KeyEventArgs = KeyEventArgs;
using Screen = Screen;

public class MessageListViewModel : Screen, IHandle<SettingsUpdatedEvent>
{
    private readonly object _deleteLockObject = new();

    private readonly ILogger _logger;

    private readonly IMessageBus _messageBus;

    private readonly MessageRepository _messageRepository;

    private readonly MessageWatcher _messageWatcher;

    private readonly MimeMessageLoader _mimeMessageLoader;

    private bool _isLoading;

    private int? _previousIndex;

    public MessageListViewModel(
        MessageRepository messageRepository,
        MessageWatcher messageWatcher,
        MimeMessageLoader mimeMessageLoader,
        IMessageBus messageBus,
        ILogger logger)
    {
        if (messageRepository == null)
            throw new ArgumentNullException(nameof(messageRepository));
        if (messageWatcher == null)
            throw new ArgumentNullException(nameof(messageWatcher));
        if (mimeMessageLoader == null)
            throw new ArgumentNullException(nameof(mimeMessageLoader));
        if (messageBus == null)
            throw new ArgumentNullException(nameof(messageBus));

        this._messageRepository = messageRepository;
        this._messageWatcher = messageWatcher;
        this._mimeMessageLoader = mimeMessageLoader;
        this._messageBus = messageBus;
        this._logger = logger;

        this.SetupMessages();
        this.RefreshMessageList();
    }

    public ObservableCollection<MimeMessageEntry> Messages { get; private set; }

    public ICollectionView MessagesSorted { get; private set; }

    public MimeMessageEntry SelectedMessage => this.GetSelected().FirstOrDefault();

    public string DeleteText => UIStrings.DeleteTextTemplate.RenderTemplate(this);

    public bool HasSelectedMessage => this.GetSelected().Any();

    public int SelectedMessageCount => this.GetSelected().Count();

    public bool IsLoading
    {
        get => this._isLoading;
        set
        {
            this._isLoading = value;
            this.NotifyOfPropertyChange(() => this.IsLoading);
        }
    }

    private ListSortDirection SortOrder => Enum.TryParse<ListSortDirection>(Settings.Default.MessageListSortOrder, out var sortOrder)
                                               ? sortOrder
                                               : ListSortDirection.Ascending;

    public Task HandleAsync(SettingsUpdatedEvent message, CancellationToken token)
    {
        this.MessagesSorted.SortDescriptions.Clear();
        this.MessagesSorted.SortDescriptions.Add(new SortDescription("ModifiedDate", this.SortOrder));

        return Task.CompletedTask;
    }

    private MimeMessageEntry GetMessageByIndex(int index)
    {
        return this.MessagesSorted.OfType<MimeMessageEntry>().Skip(index).FirstOrDefault();
    }

    private int? GetIndexOfMessage(MessageEntry? entry)
    {
        if (entry == null)
            return null;

        var index = 1; // TODO: MessagesSorted.OfType<MessageEntry>().FindIndex(m => Equals(entry, m));

        return index == -1 ? null : index;
    }

    private void PushSelectedIndex()
    {
        if (this._previousIndex.HasValue) return;

        var selectedMessage = this.SelectedMessage;

        if (selectedMessage != null) this._previousIndex = this.GetIndexOfMessage(selectedMessage);
    }

    private void PopSelectedIndex()
    {
        this._previousIndex = null;
    }

    private void SetupMessages()
    {
        this.Messages = new ObservableCollection<MimeMessageEntry>();
        this.MessagesSorted = CollectionViewSource.GetDefaultView(this.Messages);

        this.MessagesSorted.SortDescriptions.Add(new SortDescription("ModifiedDate", this.SortOrder));

        // Begin listening for new messages
        this._messageWatcher.NewMessage += this.NewMessage;

        Observable.FromEventPattern(
                e => this._messageWatcher.RefreshNeeded += e,
                e => this._messageWatcher.RefreshNeeded -= e,
                TaskPoolScheduler.Default)
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOnDispatcher()
            .Subscribe(e => this.RefreshMessageList());

        this.Messages.CollectionChanged += this.CollectionChanged;
    }

    private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
    {
        try
        {
            var notifyOfSelectionChange = new Action(
                () =>
                {
                    this.NotifyOfPropertyChange(() => this.HasSelectedMessage);
                    this.NotifyOfPropertyChange(() => this.SelectedMessageCount);
                    this.NotifyOfPropertyChange(() => this.SelectedMessage);
                    this.NotifyOfPropertyChange(() => this.DeleteText);
                });

            if (args.NewItems != null)
                foreach (var m in args.NewItems.OfType<MimeMessageEntry>())
                    m.PropertyChanged += (o, eventArgs) => notifyOfSelectionChange();

            notifyOfSelectionChange();
        }
        catch (Exception ex)
        {
            this._logger.Error(ex, "Failure Handling Message Collection Change {@Args}", args);
        }
    }

    private void AddNewMessage(MessageEntry entry)
    {
        var observable = this._mimeMessageLoader.Get(entry);

        //TODO: FIX

        //observable.ObserveOnDispatcher().Subscribe(
        //    message =>
        //    {
        //        this._messageBus.Publish(
        //            new ShowBallonTip(
        //                3500,
        //                "New Message Received",
        //                $"From: {message.From.ToString().Truncate(50)}\r\nSubject: {message.Subject.Truncate(50)}",
        //                ToolTipIcon.Info));

        //        // Add it to the list box
        //        ClearSelected();
        //        PopSelectedIndex();

        //        entry.IsSelected = true;
        //        Messages.Add(new MimeMessageEntry(entry, _mimeMessageLoader));
        //    },
        //    e =>
        //    {
        //        // NOOP
        //    });
    }

    public int? TryGetValidSelectedIndex(int? previousIndex = null)
    {
        var messageCount = this.Messages.Count;

        if (messageCount == 0) return null;

        int? index = null;

        if (previousIndex.HasValue)
        {
            index = previousIndex;

            if (index >= messageCount) index = messageCount - 1;
        }

        if (index <= 0 || index >= messageCount) index = null;

        // select the bottom
        if (!index.HasValue)
        {
            if (this.SortOrder == ListSortDirection.Ascending) index = messageCount - 1;
            else index = 0;
        }

        return index;
    }

    private void SetMessageByIndex(int index)
    {
        var m = this.GetMessageByIndex(index);
        if (m != null) m.IsSelected = true;
    }

    public void OpenMessageFolder()
    {
        string[] folders = this.GetSelected().Select(s => Path.GetDirectoryName(s.File)).Distinct().ToArray();
        folders.ForEach(f => Process.Start(f));
    }

    public void ValidateSelected()
    {
        if (this.SelectedMessageCount != 0 || this.Messages.Count == 0) return;

        var index = this.TryGetValidSelectedIndex(this._previousIndex);
        if (index.HasValue) this.SetMessageByIndex(index.Value);
    }

    private void NewMessage(object sender, NewMessageEventArgs e)
    {
        Execute.OnUIThread(() => this.AddNewMessage(e.NewMessage));
    }

    public IEnumerable<MimeMessageEntry> GetSelected()
    {
        return this.Messages.Where(message => message.IsSelected);
    }

    public void ClearSelected()
    {
        foreach (var message in this.GetSelected().ToList()) message.IsSelected = false;
    }

    public void DeleteSelected()
    {
        // Lock to prevent rapid clicking issues
        lock (this._deleteLockObject)
        {
            this.PushSelectedIndex();

            var selectedMessageEntries = this.GetSelected().ToList();

            List<string> failedEntries =
                selectedMessageEntries.Select(
                    entry =>
                    {
                        try
                        {
                            this._messageRepository.DeleteMessage(entry);
                            return null;
                        }
                        catch (Exception ex)
                        {
                            this._logger.Error(
                                ex,
                                "Failure Deleting Message {EmailMessageFile}",
                                entry.File);

                            return ex.Message;
                        }
                    }).Where(f => f != null).ToList();

            if (failedEntries.Any())
                // show errors...
                this._messageBus.Publish(
                    new ShowMessageEvent(
                        string.Join("\r\n", failedEntries),
                        $"Failed to Delete Message{(failedEntries.Count > 1 ? "s" : string.Empty)}"));
        }
    }

    public void MessageListKeyDown(KeyEventArgs e)
    {
        if (e.Key != Key.Delete)
            return;
        this.DeleteSelected();
    }

    public void RefreshMessageList()
    {
        this.PushSelectedIndex();

        var messageEntries = this._messageRepository.LoadMessages()
            .ToList();

        var toAdd =
            messageEntries.Except(this.Messages)
                .Select(m => new MimeMessageEntry(m, this._mimeMessageLoader))
                .ToList();

        var toDelete = this.Messages.Except(messageEntries).OfType<MimeMessageEntry>().ToList();
        toDelete.ForEach(m => this.Messages.Remove(m));

        this.Messages.AddRange(toAdd);

        this.MessagesSorted.Refresh();

        this.ValidateSelected();

        this.PopSelectedIndex();
    }
}