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
    using System.ComponentModel;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    using Caliburn.Micro;

    using Papercut.Common.Domain;
    using Papercut.Common.Extensions;
    using Papercut.Common.Helper;
    using Papercut.Core.Infrastructure.Network;
    using Papercut.Domain.Themes;
    using Papercut.Events;
    using Papercut.Helpers;
    using Papercut.Infrastructure.Themes;
    using Papercut.Properties;
    using Papercut.Services;

    public class OptionsViewModel : Screen
    {
        const string WindowTitleDefault = "Options";

        static readonly Lazy<IList<string>> _ipList = new Lazy<IList<string>>(GetIPs);

        readonly IMessageBus _messageBus;

        private readonly ThemeColorRepository _themeColorRepository;

        string _ip = "Any";

        bool _minimizeOnClose;

        int _port = 25;

        string _messageListSortOrder = "Descending";

        bool _runOnStartup;

        bool _startMinimized;

        string _windowTitle = WindowTitleDefault;

        private bool _minimizeToTray;

        private ThemeColor _themeColor;

        public OptionsViewModel(IMessageBus messageBus, ThemeColorRepository themeColorRepository)
        {
            _messageBus = messageBus;
            this._themeColorRepository = themeColorRepository;
            IPs = new ObservableCollection<string>(_ipList.Value);
            SortOrders = new ObservableCollection<string>(EnumHelpers.GetNames<ListSortDirection>());
            Themes = new ObservableCollection<ThemeColor>(themeColorRepository.GetAll());
            Load();
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                _windowTitle = value;
                NotifyOfPropertyChange(() => WindowTitle);
            }
        }

        public string MessageListSortOrder
        {
            get => this._messageListSortOrder;
            set
            {
                this._messageListSortOrder = value;
                NotifyOfPropertyChange(() => this.MessageListSortOrder);
            }
        }

        public ThemeColor ThemeColor
        {
            get => this._themeColor;
            set
            {
                this._themeColor = value;
                NotifyOfPropertyChange(() => this.ThemeColor);
            }
        }

        public string IP
        {
            get => _ip;
            set
            {
                _ip = value;
                NotifyOfPropertyChange(() => IP);
            }
        }

        public int Port
        {
            get => _port;
            set
            {
                _port = value;
                NotifyOfPropertyChange(() => Port);
            }
        }

        public bool RunOnStartup
        {
            get => _runOnStartup;
            set
            {
                _runOnStartup = value;
                NotifyOfPropertyChange(() => RunOnStartup);
            }
        }

        public bool MinimizeOnClose
        {
            get => _minimizeOnClose;
            set
            {
                _minimizeOnClose = value;
                NotifyOfPropertyChange(() => MinimizeOnClose);
            }
        }

        public bool MinimizeToTray
        {
            get => this._minimizeToTray;
            set
            {
                this._minimizeToTray = value;
                NotifyOfPropertyChange(() => MinimizeToTray);
            }
        }

        public bool StartMinimized
        {
            get => _startMinimized;
            set
            {
                _startMinimized = value;
                NotifyOfPropertyChange(() => StartMinimized);
            }
        }

        public ObservableCollection<string> IPs { get; private set; }

        public ObservableCollection<string> SortOrders { get; private set; }

        public ObservableCollection<ThemeColor> Themes { get; private set; }

        public void Load()
        {
            Settings.Default.CopyTo(this);

            // set the theme color
            this.ThemeColor =
                this._themeColorRepository.FirstOrDefaultByName(Settings.Default.Theme);
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

        public async Task Save()
        {
            var previousSettings = new Settings();

            Settings.Default.CopyTo(previousSettings);

            this.CopyTo(Settings.Default);

            Settings.Default.Theme = this.ThemeColor.Name;

            Settings.Default.Save();

            this._messageBus.Publish(new SettingsUpdatedEvent(previousSettings));

            await TryCloseAsync(true);
        }
    }
}