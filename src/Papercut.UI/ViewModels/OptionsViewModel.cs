// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2021 Jaben Cargman
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

    using Caliburn.Micro;

    using Papercut.Common.Domain;
    using Papercut.Common.Extensions;
    using Papercut.Common.Helper;
    using Papercut.Core.Infrastructure.Network;
    using Papercut.Domain.Events;
    using Papercut.Domain.Themes;
    using Papercut.Infrastructure.Themes;
    using Papercut.Properties;

    public class OptionsViewModel : Screen
    {
        const string WindowTitleDefault = "Options";

        static readonly Lazy<IList<string>> _ipList = new Lazy<IList<string>>(GetIPs);

        readonly IMessageBus _messageBus;

        private readonly ThemeColorRepository _themeColorRepository;

        string _ip = "Any";

        string _messageListSortOrder = "Descending";

        bool _minimizeOnClose;

        private bool _minimizeToTray;

        int _port = 25;

        bool _runOnStartup;

        bool _startMinimized;

        private ThemeColor _themeColor;

        string _windowTitle = WindowTitleDefault;
        
        private bool _showNotifications;

        public OptionsViewModel(IMessageBus messageBus, ThemeColorRepository themeColorRepository)
        {
            this._messageBus = messageBus;
            this._themeColorRepository = themeColorRepository;
            this.IPs = new ObservableCollection<string>(_ipList.Value);
            this.SortOrders = new ObservableCollection<string>(EnumHelpers.GetNames<ListSortDirection>());
            this.Themes = new ObservableCollection<ThemeColor>(themeColorRepository.GetAll());
            this.Load();
        }

        public string WindowTitle
        {
            get => this._windowTitle;
            set
            {
                this._windowTitle = value;
                this.NotifyOfPropertyChange(() => this.WindowTitle);
            }
        }

        public string MessageListSortOrder
        {
            get => this._messageListSortOrder;
            set
            {
                this._messageListSortOrder = value;
                this.NotifyOfPropertyChange(() => this.MessageListSortOrder);
            }
        }

        public ThemeColor ThemeColor
        {
            get => this._themeColor;
            set
            {
                this._themeColor = value;
                this.NotifyOfPropertyChange(() => this.ThemeColor);
            }
        }

        public string IP
        {
            get => this._ip;
            set
            {
                this._ip = value;
                this.NotifyOfPropertyChange(() => this.IP);
            }
        }

        public int Port
        {
            get => this._port;
            set
            {
                this._port = value;
                this.NotifyOfPropertyChange(() => this.Port);
            }
        }

        public bool RunOnStartup
        {
            get => this._runOnStartup;
            set
            {
                this._runOnStartup = value;
                this.NotifyOfPropertyChange(() => this.RunOnStartup);
            }
        }

        public bool MinimizeOnClose
        {
            get => this._minimizeOnClose;
            set
            {
                this._minimizeOnClose = value;
                this.NotifyOfPropertyChange(() => this.MinimizeOnClose);
            }
        }

        public bool MinimizeToTray
        {
            get => this._minimizeToTray;
            set
            {
                this._minimizeToTray = value;
                this.NotifyOfPropertyChange(() => this.MinimizeToTray);
            }
        }

        public bool ShowNotifications
        {
            get => this._showNotifications;
            set
            {
                this._showNotifications = value;
                this.NotifyOfPropertyChange(() => this.ShowNotifications);
            }
        }

        public bool StartMinimized
        {
            get => this._startMinimized;
            set
            {
                this._startMinimized = value;
                this.NotifyOfPropertyChange(() => this.StartMinimized);
            }
        }

        public ObservableCollection<string> IPs { get; }

        public ObservableCollection<string> SortOrders { get; }

        public ObservableCollection<ThemeColor> Themes { get; }

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

            await this._messageBus.PublishAsync(new SettingsUpdatedEvent(previousSettings));

            await this.TryCloseAsync(true);
        }
    }
}