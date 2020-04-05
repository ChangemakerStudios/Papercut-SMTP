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
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Windows.Data;
    using System.Windows.Forms;
    using System.Windows.Input;

    using Caliburn.Micro;

    using Message.Helpers;

    using Papercut.Common.Domain;
    using Papercut.Common.Extensions;
    using Papercut.Common.Helper;
    using Papercut.Core.Annotations;
    using Papercut.Core.Domain.Message;
    using Papercut.Events;
    using Papercut.Helpers;
    using Papercut.Message;
    using Papercut.Properties;
    using Papercut.Views;

    using Serilog;

    using Action = System.Action;
    using KeyEventArgs = System.Windows.Input.KeyEventArgs;
    using ListBox = System.Windows.Controls.ListBox;
    using Screen = Caliburn.Micro.Screen;

    public class MessageListViewModel : Screen, IHandle<SettingsUpdatedEvent>
    {
        readonly object _deleteLockObject = new object();

        readonly ILogger _logger;

        readonly MessageRepository _messageRepository;

        readonly MessageWatcher _messageWatcher;

        readonly MimeMessageLoader _mimeMessageLoader;

        readonly IMessageBus _messageBus;

        bool _isLoading;

        private int? _previousIndex;

        public MessageListViewModel(
            MessageRepository messageRepository,
            [NotNull] MessageWatcher messageWatcher,
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

            _messageRepository = messageRepository;
            _messageWatcher = messageWatcher;
            _mimeMessageLoader = mimeMessageLoader;
            this._messageBus = messageBus;
            _logger = logger;

            SetupMessages();
            RefreshMessageList();
        }

        public ObservableCollection<MimeMessageEntry> Messages { get; private set; }

        public ICollectionView MessagesSorted { get; private set; }

        public MimeMessageEntry SelectedMessage => GetSelected().FirstOrDefault();

        public string DeleteText => UIStrings.DeleteTextTemplate.RenderTemplate(this);

        public bool HasSelectedMessage => GetSelected().Any();

        public bool HasMessages => Messages.Any();

        public int SelectedMessageCount => GetSelected().Count();

        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                NotifyOfPropertyChange(() => IsLoading);
            }
        }

        MimeMessageEntry GetMessageByIndex(int index)
        {
            return MessagesSorted.OfType<MimeMessageEntry>().Skip(index).FirstOrDefault();
        }

        int? GetIndexOfMessage([CanBeNull] MessageEntry entry)
        {
            if (entry == null)
                return null;

            int index = MessagesSorted.OfType<MessageEntry>().FindIndex(m => Equals(entry, m));

            return index == -1 ? null : (int?)index;
        }

        void PushSelectedIndex()
        {
            if (this._previousIndex.HasValue)
            {
                return;
            }

            var selectedMessage = this.SelectedMessage;

            if (selectedMessage != null)
            {
                this._previousIndex = GetIndexOfMessage(selectedMessage);
            }
        }

        void PopSelectedIndex()
        {
            this._previousIndex = null;
        }

        private ListSortDirection SortOrder => Enum.TryParse<ListSortDirection>(Settings.Default.MessageListSortOrder, out var sortOrder)
                                                     ? sortOrder
                                                     : ListSortDirection.Ascending;

        void SetupMessages()
        {
            Messages = new ObservableCollection<MimeMessageEntry>();
            MessagesSorted = CollectionViewSource.GetDefaultView(Messages);
            
            MessagesSorted.SortDescriptions.Add(new SortDescription("ModifiedDate", SortOrder));

            // Begin listening for new messages
            _messageWatcher.NewMessage += NewMessage;

            Observable.FromEventPattern(
                e => _messageWatcher.RefreshNeeded += e,
                e => _messageWatcher.RefreshNeeded -= e,
                TaskPoolScheduler.Default)
                .Throttle(TimeSpan.FromMilliseconds(100))
                .ObserveOnDispatcher()
                .Subscribe(e => RefreshMessageList());

            Messages.CollectionChanged += CollectionChanged;
        }

        void CollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            try
            {
                var notifyOfSelectionChange = new Action(
                    () =>
                    {
                        NotifyOfPropertyChange(() => HasSelectedMessage);
                        NotifyOfPropertyChange(() => SelectedMessageCount);
                        NotifyOfPropertyChange(() => SelectedMessage);
                        NotifyOfPropertyChange(() => DeleteText);
                    });

                if (args.NewItems != null)
                {
                    foreach (MimeMessageEntry m in args.NewItems.OfType<MimeMessageEntry>())
                    {
                        m.PropertyChanged += (o, eventArgs) => notifyOfSelectionChange();
                    }
                }

                notifyOfSelectionChange();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failure Handling Message Collection Change {@Args}", args);
            }
        }

        void AddNewMessage(MessageEntry entry)
        {
            var observable = _mimeMessageLoader.GetObservable(entry);

            observable.ObserveOnDispatcher().Subscribe(
                message =>
                {
                    this._messageBus.Publish(
                        new ShowBallonTip(
                            3500,
                            "New Message Received",
                            $"From: {message.From.ToString().Truncate(50)}\r\nSubject: {message.Subject.Truncate(50)}",
                            ToolTipIcon.Info));

                    // Add it to the list box
                    ClearSelected();
                    PopSelectedIndex();

                    entry.IsSelected = true;
                    Messages.Add(new MimeMessageEntry(entry, _mimeMessageLoader));
                },
                e =>
                {
                    // NOOP
                });
        }

        public int? TryGetValidSelectedIndex(int? previousIndex = null)
        {
            int messageCount = Messages.Count;

            if (messageCount == 0)
            {
                return null;
            }

            int? index = null;

            if (previousIndex.HasValue)
            {
                index = previousIndex;

                if (index >= messageCount)
                {
                    index = messageCount - 1;
                }
            }

            if (index <= 0 || index >= messageCount)
            {
                index = null;
            }

            // select the bottom
            if (!index.HasValue)
            {
                if (this.SortOrder == ListSortDirection.Ascending)
                {
                    index = messageCount - 1;
                }
                else
                {
                    index = 0;
                }
            }

            return index;
        }

        private void SetMessageByIndex(int index)
        {
            MimeMessageEntry m = this.GetMessageByIndex(index);
            if (m != null)
            {
                m.IsSelected = true;
            }
        }

        public void OpenMessageFolder()
        {
            string[] folders =
                GetSelected().Select(s => Path.GetDirectoryName(s.File)).Distinct().ToArray();
            folders.ForEach(f => Process.Start(f));
        }

        public void ValidateSelected()
        {
            if (this.SelectedMessageCount != 0 || this.Messages.Count == 0) return;

            var index = this.TryGetValidSelectedIndex(this._previousIndex);
            if (index.HasValue)
            {
                this.SetMessageByIndex(index.Value);
            }
        }

        void NewMessage(object sender, NewMessageEventArgs e)
        {
            Execute.OnUIThread(() => AddNewMessage(e.NewMessage));
        }

        public IEnumerable<MimeMessageEntry> GetSelected()
        {
            return Messages.Where(message => message.IsSelected);
        }

        public void ClearSelected()
        {
            foreach (MimeMessageEntry message in GetSelected().ToList())
            {
                message.IsSelected = false;
            }
        }

        public void DeleteAll()
        {
            // Lock to prevent rapid clicking issues
            lock (_deleteLockObject)
            {
                this.ClearSelected();

                DeleteMessages(Messages.ToList());
            }
        }

        public void DeleteSelected()
        {
            // Lock to prevent rapid clicking issues
            lock (_deleteLockObject)
            {
                this.PushSelectedIndex();

                var selectedMessageEntries = this.GetSelected().ToList();

                DeleteMessages(selectedMessageEntries);
            }
        }

        private List<string> DeleteMessages(List<MimeMessageEntry> selectedMessageEntries)
        {
            List<string> failedEntries =
                selectedMessageEntries.Select(
                    entry =>
                    {
                        try
                        {
                            _messageRepository.DeleteMessage(entry);
                            return null;
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(
                                ex,
                                "Failure Deleting Message {EmailMessageFile}",
                                entry.File);

                            return ex.Message;
                        }
                    }).Where(f => f != null).ToList();

            if (failedEntries.Any())
            {
                // show errors...
                this._messageBus.Publish(
                    new ShowMessageEvent(
                        string.Join("\r\n", failedEntries),
                        $"Failed to Delete Message{(failedEntries.Count > 1 ? "s" : string.Empty)}"));
            }

            return failedEntries;
        }

        public void MessageListKeyDown(KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
                return;
            DeleteSelected();
        }

        public void RefreshMessageList()
        {
            PushSelectedIndex();

            List<MessageEntry> messageEntries =
                _messageRepository.LoadMessages()
                    .ToList();

            List<MimeMessageEntry> toAdd =
                messageEntries.Except(Messages)
                    .Select(m => new MimeMessageEntry(m, _mimeMessageLoader))
                    .ToList();

            List<MimeMessageEntry> toDelete =
                Messages.Except(messageEntries).OfType<MimeMessageEntry>().ToList();
            toDelete.ForEach(m => Messages.Remove(m));

            Messages.AddRange(toAdd);

            MessagesSorted.Refresh();

            ValidateSelected();

            PopSelectedIndex();
        }

        public void Handle(SettingsUpdatedEvent message)
        {
            MessagesSorted.SortDescriptions.Clear();
            MessagesSorted.SortDescriptions.Add(new SortDescription("ModifiedDate", this.SortOrder));
        }
    }
}