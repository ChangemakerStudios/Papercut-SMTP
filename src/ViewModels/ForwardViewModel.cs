/*  
 * Papercut
 *
 *  Copyright © 2008 - 2012 Ken Robertson
 *  Copyright © 2013 - 2014 Jaben Cargman
 *  
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *  
 *  http://www.apache.org/licenses/LICENSE-2.0
 *  
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *  
 */

namespace Papercut.ViewModels
{
    using System;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows;

    using Caliburn.Micro;

    using Papercut.Core.Message;
    using Papercut.Core.Network;
    using Papercut.Properties;

    public class ForwardViewModel : Screen, IDisposable
    {
        static readonly Regex _emailRegex =
            new Regex(
                @"(\A(\s*)\Z)|(\A([^@\s]+)@((?:[-a-z0-9]+\.)+[a-z]{2,})\Z)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        readonly MessageRepository _messageRepository;

        string _from;

        MessageEntry _messageEntry;

        bool _sending;

        string _server;

        string _to;

        string _windowTitle = "Forward Message";

        Task _worker;

        public ForwardViewModel(MessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
            Load();
        }

        public string WindowTitle
        {
            get
            {
                return _windowTitle;
            }
            set
            {
                _windowTitle = value;
                NotifyOfPropertyChange(() => WindowTitle);
            }
        }

        public string Server
        {
            get
            {
                return _server;
            }
            set
            {
                _server = value;
                NotifyOfPropertyChange(() => Server);
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

        public bool Sending
        {
            get
            {
                return _sending;
            }
            private set
            {
                _sending = value;
                NotifyOfPropertyChange(() => Sending);
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

        public MessageEntry MessageEntry
        {
            get
            {
                return _messageEntry;
            }
            set
            {
                _messageEntry = value;
                NotifyOfPropertyChange(() => MessageEntry);
            }
        }

        void Load()
        {
            // Load previous settings
            Server = Settings.Default.ForwardServer;
            To = Settings.Default.ForwardTo;
            From = Settings.Default.ForwardFrom;
        }

        public void Cancel()
        {
            TryClose(false);
        }

        public void Send()
        {
            if (string.IsNullOrEmpty(Server)
                || string.IsNullOrEmpty(From)
                || string.IsNullOrEmpty(To))
            {
                MessageBox.Show(
                    "All the text boxes are required, fill them in please.",
                    "Papercut",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!_emailRegex.IsMatch(From) || !_emailRegex.IsMatch(To))
            {
                MessageBox.Show(
                    "You need to enter valid email addresses.",
                    "Papercut",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string host = Server.Trim();
            string from = From.Trim();
            string to = To.Trim();

            _worker = Task.Factory.StartNew(
                () =>
                {
                    var session = new SmtpSession { MailFrom = from, Sender = host };
                    session.Recipients.Add(to);
                    session.Message = _messageRepository.GetMessage(MessageEntry);

                    new SmtpClient(session).Send();
                });

            _worker.ContinueWith(
                t =>
                {
                    // Save settings for the next time
                    Settings.Default.ForwardServer = Server;
                    Settings.Default.ForwardTo = To;
                    Settings.Default.ForwardFrom = From;
                    Settings.Default.Save();

                    Sending = false;

                    TryClose(true);
                },
                TaskScheduler.FromCurrentSynchronizationContext());

            Sending = true;
        }

        public void Dispose()
        {
            if (this._worker != null)
            {
                this._worker.Dispose();
            }

            this._worker = null;
        }
    }
}