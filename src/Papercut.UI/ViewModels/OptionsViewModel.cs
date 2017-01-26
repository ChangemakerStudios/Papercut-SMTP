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
    using System.Linq;
    using System.Net;

    using Caliburn.Micro;

    using Papercut.Core.Events;
    using Papercut.Core.Helper;
    using Papercut.Events;
    using Papercut.Properties;

    public class OptionsViewModel : Screen
    {
        const string WindowTitleDefault = "Options";

        static readonly Lazy<IList<string>> _ipList = new Lazy<IList<string>>(GetIPs);

        readonly IMessageBus _messageBus;

        string _ip = "Any";

        bool _minimizeOnClose;

        int _port = 25;

        bool _runOnStartup;

        bool _startMinimized;

        string _windowTitle = WindowTitleDefault;

        public OptionsViewModel(IMessageBus messageBus)
        {
            this._messageBus = messageBus;
            IPs = new ObservableCollection<string>(_ipList.Value);
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

        public void Load()
        {
            _ip = Settings.Default.IP;
            _port = Settings.Default.Port;
            _startMinimized = Settings.Default.StartMinimized;
            _minimizeOnClose = Settings.Default.MinimizeOnClose;
            _runOnStartup = Settings.Default.RunOnStartup;
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

            Settings.Default.Save();

            this._messageBus.Publish(new SettingsUpdatedEvent());

            TryClose(true);
        }
    }
}