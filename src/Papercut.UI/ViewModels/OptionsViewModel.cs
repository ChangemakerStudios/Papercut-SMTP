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


using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;

using Caliburn.Micro;

using Papercut.Common.Domain;
using Papercut.Common.Extensions;
using Papercut.Common.Helper;
using Papercut.Core.Infrastructure.Network;
using Papercut.Domain.Events;
using Papercut.Domain.Themes;
using Papercut.Infrastructure.Themes;
using Papercut.Properties;

namespace Papercut.ViewModels;

public class OptionsViewModel : Screen
{
    private const string WindowTitleDefault = "Options";

    private static readonly Lazy<IList<string>> _ipList = new(GetIPs);

    private readonly IMessageBus _messageBus;

    private readonly ThemeColorRepository _themeColorRepository;

    private bool _darkMode;

    private string _ip = "Any";

    private string _messageListSortOrder = "Descending";

    private bool _minimizeOnClose;

    private bool _minimizeToTray;

    private int _port = 25;

    private bool _runOnStartup;

    private bool _showNotifications;

    private bool _startMinimized;

    private ThemeColor _themeColor;

    private string _windowTitle = WindowTitleDefault;

    public OptionsViewModel(IMessageBus messageBus, ThemeColorRepository themeColorRepository)
    {
        _messageBus = messageBus;
        _themeColorRepository = themeColorRepository;
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
        get => _messageListSortOrder;
        set
        {
            _messageListSortOrder = value;
            NotifyOfPropertyChange(() => MessageListSortOrder);
        }
    }

    public ThemeColor ThemeColor
    {
        get => _themeColor;
        set
        {
            _themeColor = value;
            NotifyOfPropertyChange(() => ThemeColor);
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
        get => _minimizeToTray;
        set
        {
            _minimizeToTray = value;
            NotifyOfPropertyChange(() => MinimizeToTray);
        }
    }

    public bool ShowNotifications
    {
        get => _showNotifications;
        set
        {
            _showNotifications = value;
            NotifyOfPropertyChange(() => ShowNotifications);
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

    public bool DarkMode
    {
        get => _darkMode;
        set
        {
            _darkMode = value;
            NotifyOfPropertyChange(() => DarkMode);
        }
    }

    public ObservableCollection<string> IPs { get; }

    public ObservableCollection<string> SortOrders { get; }

    public ObservableCollection<ThemeColor> Themes { get; }

    public void Load()
    {
        Settings.Default.CopyTo(this);

        // set the theme color
        ThemeColor =
            _themeColorRepository.FirstOrDefaultByName(Settings.Default.Theme) ?? ThemeColorRepository.Default;
    }

    private static IList<string> GetIPs()
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

        Settings.Default.Theme = ThemeColor.Name;
        Settings.Default.DarkMode = DarkMode;

        Settings.Default.Save();

        await _messageBus.PublishAsync(new SettingsUpdatedEvent(previousSettings));

        await TryCloseAsync(true);
    }
}