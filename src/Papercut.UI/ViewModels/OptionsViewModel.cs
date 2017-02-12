// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2016 Jaben Cargman
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
    using System.ComponentModel;
    using System.Linq;
    using System.Net;

    using Caliburn.Micro;

    using Papercut.Core.Events;
    using Papercut.Core.Helper;
    using Papercut.Events;
    using Papercut.Helpers;
    using Papercut.Properties;

    public class OptionsViewModel : Screen
    {
        const string WindowTitleDefault = "Options";

        static readonly Lazy<IList<string>> _ipList = new Lazy<IList<string>>(GetIPs);

        readonly IMessageBus _messageBus;

        string _ip = "Any";

        bool _minimizeOnClose;

        int _port = 25;

        string _messageListSortOrder = "Descending";

        bool _runOnStartup;

        bool _startMinimized;

        string _windowTitle = WindowTitleDefault;

        private bool _minimizeToTray;

        private string _theme;

        public OptionsViewModel(IMessageBus messageBus)
        {
            _messageBus = messageBus;
            IPs = new ObservableCollection<string>(_ipList.Value);
            SortOrders = new ObservableCollection<string>(Enum.GetNames(typeof(ListSortDirection)));
            Themes = new ObservableCollection<string>(Enum.GetNames(typeof(Themes)));
            Load();
        }

        public string WindowTitle
        {
            get { return _windowTitle; }
            set
            {
                _windowTitle = value;
                NotifyOfPropertyChange(() => WindowTitle);
            }
        }

        public string MessageListSortOrder
        {
            get { return this._messageListSortOrder; }
            set
            {
                this._messageListSortOrder = value;
                NotifyOfPropertyChange(() => this.MessageListSortOrder);
            }
        }

        public string Theme
        {
            get
            {
                return this._theme;
            }
            set
            {
                this._theme = value;
                NotifyOfPropertyChange(() => this.Theme);
            }
        }

        public string IP
        {
            get { return _ip; }
            set
            {
                _ip = value;
                NotifyOfPropertyChange(() => IP);
            }
        }

        public int Port
        {
            get { return _port; }
            set
            {
                _port = value;
                NotifyOfPropertyChange(() => Port);
            }
        }

        public bool RunOnStartup
        {
            get { return _runOnStartup; }
            set
            {
                _runOnStartup = value;
                NotifyOfPropertyChange(() => RunOnStartup);
            }
        }

        public bool MinimizeOnClose
        {
            get { return _minimizeOnClose; }
            set
            {
                _minimizeOnClose = value;
                NotifyOfPropertyChange(() => MinimizeOnClose);
            }
        }

        public bool MinimizeToTray
        {
            get
            {
                return this._minimizeToTray;
            }
            set
            {
                this._minimizeToTray = value;
                NotifyOfPropertyChange(() => MinimizeToTray);
            }
        }

        public bool StartMinimized
        {
            get { return _startMinimized; }
            set
            {
                _startMinimized = value;
                NotifyOfPropertyChange(() => StartMinimized);
            }
        }

        public ObservableCollection<string> IPs { get; private set; }

        public ObservableCollection<string> SortOrders { get; private set; }

        public ObservableCollection<string> Themes { get; private set; }

        public void Load()
        {
            _ip = Settings.Default.IP;
            _port = Settings.Default.Port;

            _messageListSortOrder = Settings.Default.MessageListSortOrder;
            _startMinimized = Settings.Default.StartMinimized;
            _minimizeToTray = Settings.Default.MinimizeToTray;
            _minimizeOnClose = Settings.Default.MinimizeOnClose;
            _runOnStartup = Settings.Default.RunOnStartup;
            _theme = Settings.Default.Theme;
        }

        static IList<string> GetIPs()
        {
            var ips = new List<string> { "Any" };

            ips.AddRange(
                Dns.GetHostAddresses("localhost")
                    .Select(a => a.ToString())
                    .Where(NetworkHelper.IsValidIP));

            ips.AddRange(NetworkHelper.GetIPAddresses().Where(NetworkHelper.IsValidIP));

            return ips;
        }

        public void Save()
        {
            Settings.Default.IP = IP;
            Settings.Default.Port = Port;

            Settings.Default.RunOnStartup = RunOnStartup;
            Settings.Default.StartMinimized = StartMinimized;
            Settings.Default.MinimizeOnClose = MinimizeOnClose;
            Settings.Default.MinimizeToTray = MinimizeToTray;
            Settings.Default.MessageListSortOrder = MessageListSortOrder;
            Settings.Default.Theme = Theme;

            Settings.Default.Save();

            this._messageBus.Publish(new SettingsUpdatedEvent());

            TryClose(true);
        }
    }
}