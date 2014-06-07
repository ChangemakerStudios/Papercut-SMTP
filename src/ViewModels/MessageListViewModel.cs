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
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Forms;
    using System.Windows.Input;
    using System.Windows.Navigation;

    using Caliburn.Micro;

    using Papercut.Core.Events;
    using Papercut.Core.Helper;
    using Papercut.Core.Message;
    using Papercut.Events;
    using Papercut.Helpers;

    using Serilog;

    using Action = System.Action;
    using DataFormats = System.Windows.DataFormats;
    using DataObject = System.Windows.DataObject;
    using DragDropEffects = System.Windows.DragDropEffects;
    using KeyEventArgs = System.Windows.Input.KeyEventArgs;
    using ListBox = System.Windows.Controls.ListBox;
    using MouseEventArgs = System.Windows.Input.MouseEventArgs;
    using Screen = Caliburn.Micro.Screen;
    using ScrollBar = System.Windows.Controls.Primitives.ScrollBar;

    public class MessageListViewModel : Screen
    {
        readonly ILogger _logger;

        readonly MessageRepository _messageRepository;

        readonly MimeMessageLoader _mimeMessageLoader;

        readonly IPublishEvent _publishEvent;

        readonly object _deleteLockObject = new object();

        

        public MessageListViewModel(
            MessageRepository messageRepository,
            MimeMessageLoader mimeMessageLoader,
            IPublishEvent publishEvent,
            ILogger logger)
        {
            if (messageRepository == null) throw new ArgumentNullException("messageRepository");
            if (mimeMessageLoader == null) throw new ArgumentNullException("mimeMessageLoader");
            if (publishEvent == null) throw new ArgumentNullException("publishEvent");

            _messageRepository = messageRepository;
            _mimeMessageLoader = mimeMessageLoader;
            _publishEvent = publishEvent;
            _logger = logger;

            SetupMessages();
            RefreshMessageList();
        }

        public ObservableCollection<MimeMessageEntry> Messages { get; private set; }

        public ICollectionView MessagesSorted { get; private set; }

        public MimeMessageEntry SelectedMessage
        {
            get
            {
                return GetSelected().FirstOrDefault();
            }
        }

        public bool HasSelectedMessage
        {
            get
            {
                return GetSelected().Any();
            }
        }

        public int SelectedMessageCount
        {
            get
            {
                return GetSelected().Count();
            }
        }

        private MimeMessageEntry GetMessageByIndex(int index)
        {
            return MessagesSorted.OfType<MimeMessageEntry>().Skip(index).FirstOrDefault();
        }

        private int? GetIndexOfMessage(MessageEntry entry)
        {
            if (entry == null) throw new ArgumentNullException("entry");

            int index = 0;
            foreach (var message in MessagesSorted.OfType<MimeMessageEntry>())
            {
                if (Equals(entry, message)) return index;
                index++;
            }

            return null;
        }

        void SetupMessages()
        {
            Messages = new ObservableCollection<MimeMessageEntry>();
            MessagesSorted = CollectionViewSource.GetDefaultView(Messages);
            MessagesSorted.SortDescriptions.Add(
                new SortDescription("ModifiedDate", ListSortDirection.Ascending));

            // Begin listening for new messages
            _messageRepository.NewMessage += NewMessage;
            _messageRepository.RefreshNeeded += RefreshMessages;

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
                    });

                if (args.NewItems != null)
                {
                    foreach (var m in args.NewItems.OfType<MimeMessageEntry>())
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

        void RefreshMessages(object sender, EventArgs e)
        {
            Execute.OnUIThread(RefreshMessageList);
        }

        void AddNewMessage(MessageEntry entry)
        {
            _mimeMessageLoader.Get(entry)
                .ObserveOnDispatcher()
                .Subscribe(
                    message =>
                    {
                        _publishEvent.Publish(
                            new ShowBallonTip(
                                5000,
                                "New Message Received",
                                string.Format(
                                    "From: {0}\r\nSubject: {1}",
                                    message.From.ToString().Truncate(50),
                                    message.Subject.Truncate(50)),
                                ToolTipIcon.Info));

                        // Add it to the list box
                        ClearSelected();
                        entry.IsSelected = true;
                        Messages.Add(new MimeMessageEntry(entry, this._mimeMessageLoader));
                    });
        }

        public void UpdateSelectedIndex(int? index = null)
        {
            ClearSelected();

            int messageCount = Messages.Count;

            if (index.HasValue && index >= messageCount)
            {
                index = null;
            }

            if (!index.HasValue && messageCount > 0)
            {
                index = messageCount - 1;
            }

            if (index.HasValue)
            {
                var m = GetMessageByIndex(index.Value);
                if (m != null) m.IsSelected = true;
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
            foreach (var message in GetSelected().ToList())
            {
                message.IsSelected = false;
            }
        }

        public void DeleteSelected()
        {
            int? previousIndex = null;

            // Lock to prevent rapid clicking issues
            lock (_deleteLockObject)
            {
                var selectedList = GetSelected().ToList();

                var messageEntry = selectedList.FirstOrDefault();
                if (messageEntry != null)
                {
                    previousIndex = GetIndexOfMessage(messageEntry);
                }

                foreach (var entry in selectedList)
                {
                    _messageRepository.DeleteMessage(entry);
                    Messages.Remove(entry);
                }
            }

            Execute.OnUIThread(
                () =>
                {
                    MessagesSorted.Refresh();
                    UpdateSelectedIndex(previousIndex);
                });
        }

        public void MessageListKeyDown(KeyEventArgs e)
        {
            if (e.Key != Key.Delete) return;
            DeleteSelected();
        }

        

        public void RefreshMessageList()
        {
            var messageEntries =
                _messageRepository.LoadMessages()
                    .ToList();
            
            var toAdd =
                messageEntries.Except(Messages)
                    .Select(m => new MimeMessageEntry(m, _mimeMessageLoader))
                    .ToList();

            var toDelete = Messages.Except(messageEntries).OfType<MimeMessageEntry>().ToList();
            toDelete.ForEach(m => Messages.Remove(m));

            Messages.AddRange(toAdd);

            UpdateSelectedIndex();
        }
    }
}