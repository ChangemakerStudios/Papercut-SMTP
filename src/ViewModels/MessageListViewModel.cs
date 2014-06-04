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

        Point? _dragStartPoint;

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
        }

        public ObservableCollection<MessageEntry> Messages { get; private set; }

        public ICollectionView MessagesSorted { get; private set; }

        public MessageEntry SelectedMessage
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

        private MessageEntry GetMessageByIndex(int index)
        {
            return MessagesSorted.OfType<MessageEntry>().Skip(index).FirstOrDefault();
        }

        private int? GetIndexOfMessage(MessageEntry entry)
        {
            if (entry == null) throw new ArgumentNullException("entry");

            int index = 0;
            foreach (var message in MessagesSorted.OfType<MessageEntry>())
            {
                if (Equals(message, entry)) return index;
                index++;
            }

            return null;
        }

        void SetupMessages()
        {
            Messages = new ObservableCollection<MessageEntry>();
            MessagesSorted = CollectionViewSource.GetDefaultView(Messages);
            MessagesSorted.SortDescriptions.Add(
                new SortDescription("ModifiedDate", ListSortDirection.Ascending));

            // Begin listening for new messages
            _messageRepository.NewMessage += NewMessage;
            _messageRepository.RefreshNeeded += RefreshMessages;

            Messages.CollectionChanged += CollectionChanged;

            RefreshMessageList();
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
                    foreach (var m in args.NewItems.OfType<MessageEntry>())
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
                        Messages.Add(entry);
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

        public IEnumerable<MessageEntry> GetSelected()
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

        /// <summary>
        ///     Handles the OnKeyDown event of the MessagesList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.KeyEventArgs" /> instance containing the event data.</param>
        void MessagesList_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete) return;

            DeleteSelected();
        }

        void MessagesList_OnPreviewLeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            var parent = sender as ListBox;

            if (parent == null) return;

            if (_dragStartPoint == null) _dragStartPoint = e.GetPosition(parent);
        }

        void MessagesList_OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            var parent = sender as ListBox;
            if (parent == null || _dragStartPoint == null) return;

            if (((DependencyObject)e.OriginalSource).FindAncestor<ScrollBar>() != null) return;

            Point dragPoint = e.GetPosition(parent);

            Vector potentialDragLength = dragPoint - _dragStartPoint.Value;

            if (potentialDragLength.Length > 10)
            {
                // Get the object source for the selected item
                var entry = parent.GetObjectDataFromPoint<MessageEntry>(_dragStartPoint.Value);

                // If the data is not null then start the drag drop operation
                if (entry != null && !string.IsNullOrWhiteSpace(entry.File))
                {
                    var dataObject = new DataObject(DataFormats.FileDrop, new[] { entry.File });
                    DragDrop.DoDragDrop(parent, dataObject, DragDropEffects.Copy);
                }

                _dragStartPoint = null;
            }
        }

        void MessagesList_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = null;
        }

        public void RefreshMessageList()
        {
            IList<MessageEntry> messageEntries = _messageRepository.LoadMessages();

            Messages.Clear();
            Messages.AddRange(messageEntries);

            UpdateSelectedIndex();
        }
    }
}