// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2026 Jaben Cargman
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


using System.Diagnostics;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

using Papercut.Desktop.Platform;
using Papercut.Desktop.Services;

namespace Papercut.Desktop;

/// <summary>
/// The tray icon and its menu, and the wiring between the service source, the window,
/// notifications and updates. The window opens on demand, not at startup.
/// </summary>
internal sealed class App : Application
{
    private readonly CancellationTokenSource _shutdown = new();

    private ShellSettings _settings = null!;

    private ServiceSupervisor _supervisor = null!;

    private MessagesHubClient _hub = null!;

    // One instance each, with only their items replaced: macOS binds to the first menu it
    // exports and throws on any other
    private readonly NativeMenu _trayMenu = new();

    private readonly NativeMenu _windowMenu = new();

    private INotifier _notifier = null!;

    private IRunAtStartup _runAtStartup = null!;

    private ShellUpdater _updater = null!;

    private IClassicDesktopStyleApplicationLifetime _lifetime = null!;

    private TrayIcon _tray = null!;

    private MainWindow? _window;

    private DispatcherTimer? _timer;

    private bool _exiting;

    private string? _menuState;

    public static ShellSettings? StartupSettings { get; set; }

    public static SingleInstance? Instance { get; set; }

    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }

        _lifetime = lifetime;
        _settings = StartupSettings ?? ShellSettings.Load();
        _notifier = PlatformServices.CreateNotifier();
        _runAtStartup = PlatformServices.CreateRunAtStartup();
        _updater = new ShellUpdater();
        _supervisor = new ServiceSupervisor(_settings, new DesktopServiceHost(_settings), PlatformServices.CreateWindowsServiceControl());
        _hub = new MessagesHubClient();

        _supervisor.Changed += () => Dispatcher.UIThread.Post(OnServiceChanged);
        _hub.NewMessageReceived += OnNewMessage;
        _updater.UpdateReady += () => Dispatcher.UIThread.Post(OnUpdateReady);

        _tray = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Papercut/Assets/papercut.ico"))),
            ToolTipText = "Papercut SMTP",
            // before SetIcons: attaching without a menu exports an empty one, which macOS keeps
            Menu = _trayMenu
        };
        _tray.Clicked += (_, _) => ShowWindow();
        TrayIcon.SetIcons(this, new TrayIcons { _tray });
        RebuildMenu();

        lifetime.ShutdownRequested += (_, _) => _ = StopServicesAsync();

        Instance?.Listen(() => Dispatcher.UIThread.Post(ShowWindow));

        _timer = new DispatcherTimer(TimeSpan.FromSeconds(5), DispatcherPriority.Background, async (_, _) =>
        {
            await _supervisor.CheckAsync(_shutdown.Token);
            RebuildMenu();
        });
        _timer.Start();

        _ = _supervisor.ResolveAsync(_shutdown.Token);
        _updater.StartChecking(_shutdown.Token);

        if (_settings.ShowV7RulesNotice)
        {
            _notifier.Show("Papercut SMTP", "Rules from Papercut 7 couldn't be carried over. If you used forwarding rules, re-create them under Rules.", null);
            _settings.ShowV7RulesNotice = false;
            _settings.Save();
        }

        Log.Information("Papercut desktop {Version} started", _updater.CurrentVersion ?? "(not installed)");

        base.OnFrameworkInitializationCompleted();
    }

    private void OnServiceChanged()
    {
        var uri = _supervisor.IsStarting ? null : _supervisor.BaseUri;

        _window?.SetService(uri, _supervisor.StatusText);
        _ = _hub.ConnectAsync(uri);

        _tray.ToolTipText = $"Papercut SMTP - {_supervisor.StatusText}";
        RebuildMenu();
    }

    private void OnNewMessage(NewMessageDto message)
    {
        if (!_settings.ShowNotifications) return;

        var subject = string.IsNullOrWhiteSpace(message.Subject) ? "(No Subject)" : message.Subject;
        var id = message.Id;

        Log.Information("Notifying new message {Subject} from {From}", subject, message.FromDisplay);

        _notifier.Show(message.FromDisplay, subject, id == null ? null : () => Dispatcher.UIThread.Post(() => ShowMessage(id)));
    }

    private void OnUpdateReady()
    {
        RebuildMenu();
        _notifier.Show("Update ready", $"Papercut {_updater.PendingUpdate?.Version} is downloaded. Restart from the tray menu to use it.", null);
    }

    private void ShowWindow()
    {
        if (_window == null)
        {
            _window = new MainWindow(_settings, () => _ = _supervisor.ResolveAsync(_shutdown.Token));
            _window.OpenExternallyRequested += OpenExternally;
            _window.SetService(_supervisor.IsStarting ? null : _supervisor.BaseUri, _supervisor.StatusText);
            _window.SetMenu(_windowMenu);
        }

        _window.Show();
        _window.WindowState = _window.WindowState == WindowState.Minimized ? WindowState.Normal : _window.WindowState;
        _window.Activate();
    }

    private void ShowMessage(string id)
    {
        ShowWindow();
        _window?.ShowMessage(id);
    }

    /// <summary>
    /// Rebuilds the tray and window menus when what they show has changed. Rebuilding
    /// every tick would close a window menu the user has open.
    /// </summary>
    private void RebuildMenu(bool force = false)
    {
        var state = string.Join('|',
            _supervisor?.StatusText, _supervisor?.BaseUri, _supervisor?.IsStarting,
            _supervisor?.IsWindowsServiceInstalled, _supervisor?.IsWindowsServiceRunning,
            _updater?.PendingUpdate?.Version, SafeIsRunAtStartup(), _settings.ShowNotifications);

        if (!force && state == _menuState) return;
        _menuState = state;

        FillTrayMenu();
        FillWindowMenu();
    }

    private void FillTrayMenu()
    {
        var menu = _trayMenu;
        menu.Items.Clear();

        menu.Add(new NativeMenuItem($"Papercut SMTP: {_supervisor?.StatusText ?? "Starting..."}") { IsEnabled = false });
        menu.Add(new NativeMenuItemSeparator());

        menu.Add(Item("Open Papercut", ShowWindow));
        menu.Add(OpenInBrowserItem());

        if (WindowsServiceItem() is { } serviceItem)
        {
            menu.Add(new NativeMenuItemSeparator());
            menu.Add(serviceItem);
        }

        menu.Add(new NativeMenuItemSeparator());

        if (RestartToUpdateItem() is { } updateItem) menu.Add(updateItem);

        menu.Add(RunAtStartupItem());
        menu.Add(ShowNotificationsItem());
        menu.Add(Item("View Logs", () => OpenPath(ShellPaths.LogDirectory)));
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(Item("Exit", () => _ = ExitAsync()));
    }

    private void FillWindowMenu()
    {
        var file = new NativeMenu();
        file.Add(OpenInBrowserItem());
        file.Add(Item("Open Data Folder", () => OpenPath(ShellPaths.DataDirectory)));
        file.Add(Item("View Logs", () => OpenPath(ShellPaths.LogDirectory)));
        file.Add(new NativeMenuItemSeparator());
        file.Add(Item("Close Window", () => _window?.Close()));
        file.Add(Item("Exit", () => _ = ExitAsync()));

        var view = new NativeMenu();
        view.Add(Item("Reload", () => _window?.Reload()));

        var options = new NativeMenu();
        options.Add(RunAtStartupItem());
        options.Add(ShowNotificationsItem());

        if (WindowsServiceItem() is { } serviceItem)
        {
            options.Add(new NativeMenuItemSeparator());
            options.Add(serviceItem);
        }

        var help = new NativeMenu();
        help.Add(Item("Papercut SMTP on GitHub", () => OpenExternally(new Uri(ShellUpdater.RepositoryUrl))));
        help.Add(Item("Report an Issue", () => OpenExternally(new Uri(ShellUpdater.RepositoryUrl + "/issues"))));
        help.Add(new NativeMenuItemSeparator());
        help.Add(RestartToUpdateItem() ?? Item("Check for Updates", () => _ = CheckForUpdatesAsync()));
        help.Add(new NativeMenuItemSeparator());
        help.Add(new NativeMenuItem($"Papercut SMTP {Version}") { IsEnabled = false });

        _windowMenu.Items.Clear();
        _windowMenu.Add(new NativeMenuItem("_File") { Menu = file });
        _windowMenu.Add(new NativeMenuItem("_View") { Menu = view });
        _windowMenu.Add(new NativeMenuItem("_Options") { Menu = options });
        _windowMenu.Add(new NativeMenuItem("_Help") { Menu = help });
    }

    private NativeMenuItem OpenInBrowserItem() =>
        Item("Open in Browser", () =>
        {
            if (_supervisor?.BaseUri is { } baseUri) OpenExternally(baseUri);
        }, enabled: _supervisor?.BaseUri != null && !_supervisor.IsStarting);

    private NativeMenuItem? WindowsServiceItem()
    {
        if (_supervisor?.IsWindowsServiceInstalled != true) return null;

        return _supervisor.IsWindowsServiceRunning
            ? Item("Stop Windows Service", () => _ = _supervisor.StopWindowsServiceAsync(), enabled: !_supervisor.IsStarting)
            : Item("Start Windows Service", () => _ = _supervisor.StartWindowsServiceAsync(), enabled: !_supervisor.IsStarting);
    }

    private NativeMenuItem? RestartToUpdateItem() =>
        _updater?.PendingUpdate is { } pending
            ? Item($"Restart to Update ({pending.Version})", () => _ = RestartToUpdateAsync())
            : null;

    private NativeMenuItem RunAtStartupItem() =>
        Toggle("Run at Startup", SafeIsRunAtStartup(), on =>
        {
            try
            {
                _runAtStartup.Set(on);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to change run at startup");
                _notifier.Show("Papercut SMTP", $"Could not change Run at Startup: {ex.Message}", null);
            }

            RebuildMenu();
        });

    private NativeMenuItem ShowNotificationsItem() =>
        Toggle("Show Notifications", _settings.ShowNotifications, on =>
        {
            _settings.ShowNotifications = on;
            _settings.Save();
            RebuildMenu();
        });

    private async Task CheckForUpdatesAsync()
    {
        var result = await _updater.CheckAsync();

        // Ready raises UpdateReady, which has its own notification
        var message = result switch
        {
            UpdateCheckResult.NotInstalled => "Updates are only available in the installed app.",
            UpdateCheckResult.UpToDate => $"Papercut SMTP {Version} is up to date.",
            UpdateCheckResult.Failed => "Could not check for updates. See the logs for details.",
            _ => null
        };

        if (message != null) _notifier.Show("Papercut SMTP", message, null);
    }

    private string Version =>
        _updater.CurrentVersion
        ?? typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0]
        ?? "(dev)";

    private bool SafeIsRunAtStartup()
    {
        try
        {
            return _runAtStartup.IsEnabled;
        }
        catch
        {
            return false;
        }
    }

    private static NativeMenuItem Item(string header, Action onClick, bool enabled = true)
    {
        var item = new NativeMenuItem(header) { IsEnabled = enabled };
        item.Click += (_, _) => onClick();
        return item;
    }

    private static NativeMenuItem Toggle(string header, bool isChecked, Action<bool> onChanged)
    {
        var item = new NativeMenuItem(header) { ToggleType = MenuItemToggleType.CheckBox, IsChecked = isChecked };
        item.Click += (_, _) => onChanged(!isChecked);
        return item;
    }

    private async Task RestartToUpdateAsync()
    {
        // the desktop service holds files in current\
        await StopServicesAsync();
        _updater.ApplyAndRestart();
    }

    private async Task ExitAsync()
    {
        if (_exiting) return;
        _exiting = true;

        Log.Information("Exiting");

        _window?.AllowClose();
        _window?.Close();

        await StopServicesAsync();
        _updater.ApplyOnExit();

        _lifetime.Shutdown();
    }

    private async Task StopServicesAsync()
    {
        _timer?.Stop();
        await _shutdown.CancelAsync();
        await _hub.DisposeAsync();
        await _supervisor.DisposeAsync();
    }

    private static void OpenExternally(Uri uri)
    {
        // LinkPolicy only lets http(s) and mailto get here
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeMailto) return;

        Log.Information("Opening {Uri} outside Papercut", uri);
        OpenPath(uri.AbsoluteUri);
    }

    private static void OpenPath(string target)
    {
        try
        {
            if (OperatingSystem.IsMacOS())
            {
                var startInfo = new ProcessStartInfo("open") { UseShellExecute = false };
                startInfo.ArgumentList.Add(target);
                Process.Start(startInfo)?.Dispose();
            }
            else
            {
                Directory.CreateDirectory(ShellPaths.LogDirectory);
                Process.Start(new ProcessStartInfo(target) { UseShellExecute = true })?.Dispose();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Unable to open {Target}", target);
        }
    }
}
