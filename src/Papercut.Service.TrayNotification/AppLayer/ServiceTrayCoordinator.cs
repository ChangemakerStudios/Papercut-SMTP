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


using System.Diagnostics;
using System.ServiceProcess;

using Autofac;

using Papercut.Core.Domain.Paths;
using Papercut.Service.TrayNotification.Infrastructure;

namespace Papercut.Service.TrayNotification.AppLayer;

public class ServiceTrayCoordinator : IDisposable
{
    private readonly AppRunOnStartupService _appRunOnStartupService;

    private readonly LoggingPathConfigurator _loggingPathConfigurator;

    private readonly NotifyIcon _notifyIcon;

    private readonly ServiceStatusService _serviceStatusService;

    private readonly NewMessageNotificationService _notificationService;

    private readonly System.Windows.Forms.Timer _statusUpdateTimer;

    private readonly Icon _trayIcon;

    public ServiceTrayCoordinator(
        LoggingPathConfigurator loggingPathConfigurator,
        ServiceStatusService serviceStatusService,
        AppRunOnStartupService appRunOnStartupService,
        NewMessageNotificationService notificationService)
    {
        _loggingPathConfigurator = loggingPathConfigurator;
        _serviceStatusService = serviceStatusService;
        _appRunOnStartupService = appRunOnStartupService;
        _notificationService = notificationService;

        // Load icon once to avoid GDI handle leaks
        _trayIcon = LoadIcon();

        _notifyIcon = new NotifyIcon
        {
            Icon = _trayIcon,
            Text = "Papercut SMTP Service Manager",
            Visible = true
        };

        _notifyIcon.DoubleClick += OnTrayIconDoubleClick;
        _notifyIcon.BalloonTipClicked += OnBalloonTipClicked;
        _notifyIcon.ContextMenuStrip = CreateContextMenu();

        // Subscribe to status changes
        _serviceStatusService.StatusChanged += OnServiceStatusChanged;

        // Subscribe to new message notifications
        _notificationService.NewMessageReceived += OnNewMessageReceived;

        // Update service status every 2 seconds
        _statusUpdateTimer = new System.Windows.Forms.Timer
        {
            Interval = 2000
        };
        _statusUpdateTimer.Tick += (_, _) => _serviceStatusService.UpdateStatus();
        _statusUpdateTimer.Start();

        // Initial status update
        _serviceStatusService.UpdateStatus();

        Log.Information("Service Tray Coordinator initialized");
    }

    public void Dispose()
    {
        _serviceStatusService.StatusChanged -= OnServiceStatusChanged;
        _notificationService.NewMessageReceived -= OnNewMessageReceived;
        _statusUpdateTimer?.Stop();
        _statusUpdateTimer?.Dispose();
        _notifyIcon?.Dispose();
        _trayIcon?.Dispose();
    }

    private void OnNewMessageReceived(object? sender, Core.Domain.Message.NewMessageEvent e)
    {
        if (!_notificationService.NotificationsEnabled)
            return;

        try
        {
            // Extract subject from filename format: {timestamp} {subject} {randomstring}.eml
            var fileName = Path.GetFileNameWithoutExtension(e.NewMessage.Name);
            var parts = fileName.Split(' ', 3); // Split into timestamp, subject, random
            var subject = parts.Length >= 2
                ? string.Join(" ", parts.Skip(1).Take(parts.Length - 2))
                : "(No Subject)";

            if (string.IsNullOrWhiteSpace(subject))
                subject = "(No Subject)";

            ShowBalloonTip(
                "New Email Received",
                $"Subject: {subject}",
                ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to show new message notification");
        }
    }

    private async void OnServiceStatusChanged(object? sender, ServiceControllerStatus status)
    {
        UpdateTrayIcon();
    }

    private Icon LoadIcon()
    {
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "icons", "Papercut-icon.ico");
            if (File.Exists(iconPath))
            {
                return new Icon(iconPath);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load custom icon, using default");
        }

        // Fallback to default application icon
        return SystemIcons.Application;
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();

        var statusItem = new ToolStripLabel("Checking service status...")
        {
            Name = "statusLabel",
            Font = new Font(menu.Font, FontStyle.Bold)
        };
        menu.Items.Add(statusItem);
        menu.Items.Add(new ToolStripSeparator());

        var startItem = new ToolStripMenuItem("Start Service", null, OnStartService)
        {
            Name = "startService"
        };
        menu.Items.Add(startItem);

        var stopItem = new ToolStripMenuItem("Stop Service", null, OnStopService)
        {
            Name = "stopService"
        };
        menu.Items.Add(stopItem);

        var restartItem = new ToolStripMenuItem("Restart Service", null, OnRestartService)
        {
            Name = "restartService"
        };
        menu.Items.Add(restartItem);

        menu.Items.Add(new ToolStripSeparator());

        var openWebUIItem = new ToolStripMenuItem("Open Web UI", null, OnOpenWebUI)
        {
            Name = "openWebUI"
        };
        menu.Items.Add(openWebUIItem);
        menu.Items.Add("View Logs Folder", null, OnViewLogs);

        menu.Items.Add(new ToolStripSeparator());

        var runOnStartupItem = new ToolStripMenuItem("Run at Startup", null, OnToggleRunOnStartup)
        {
            Name = "runOnStartup",
            CheckOnClick = true
        };
        menu.Items.Add(runOnStartupItem);

        var showNotificationsItem = new ToolStripMenuItem("Show Notifications", null, OnToggleNotifications)
        {
            Name = "showNotifications",
            CheckOnClick = true,
            Checked = true // Default to enabled
        };
        menu.Items.Add(showNotificationsItem);

        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add("Exit", null, OnExit);

        // Update menu state before showing
        menu.Opening += (_, _) => UpdateMenuState(menu);

        return menu;
    }

    private void UpdateTrayIcon()
    {
        var statusText = _serviceStatusService.GetStatusText();
        _notifyIcon.Text = $"Papercut SMTP Service ({statusText})";

        // Icon is already set during construction and reused to avoid GDI handle leaks
        // Could add different icons or overlays based on status in the future
    }

    private void UpdateMenuState(ContextMenuStrip menu)
    {
        if (menu.Items["statusLabel"] is not ToolStripLabel statusLabel
            || menu.Items["startService"] is not ToolStripMenuItem startItem
            || menu.Items["stopService"] is not ToolStripMenuItem stopItem
            || menu.Items["restartService"] is not ToolStripMenuItem restartItem
            || menu.Items["openWebUI"] is not ToolStripMenuItem openWebUIItem
            || menu.Items["runOnStartup"] is not ToolStripMenuItem runOnStartupItem
            || menu.Items["showNotifications"] is not ToolStripMenuItem showNotificationsItem)
            return;

        // Update Run on Startup checkbox
        runOnStartupItem.Checked = _appRunOnStartupService.IsRunOnStartupEnabled();

        // Update Show Notifications checkbox
        showNotificationsItem.Checked = _notificationService.NotificationsEnabled;

        if (!_serviceStatusService.IsServiceInstalled)
        {
            statusLabel.Text = "✗ Service Not Installed";
            startItem.Enabled = false;
            stopItem.Enabled = false;
            restartItem.Enabled = false;
            openWebUIItem.Enabled = false;
            openWebUIItem.Text = "Open Web UI";
            return;
        }

        var status = _serviceStatusService.CurrentStatus;
        statusLabel.Text = status switch
        {
            ServiceControllerStatus.Running => "● Service Running",
            ServiceControllerStatus.Stopped => "○ Service Stopped",
            ServiceControllerStatus.StartPending => "◐ Service Starting...",
            ServiceControllerStatus.StopPending => "◑ Service Stopping...",
            null => "? Service Status Unknown",
            _ => $"◌ Service {status}"
        };

        startItem.Enabled = _serviceStatusService.CanStart();
        stopItem.Enabled = _serviceStatusService.CanStop();
        restartItem.Enabled = _serviceStatusService.CanRestart();

        // Update Open Web UI menu item with URL and enable only when service is running
        var isRunning = status == ServiceControllerStatus.Running;
        openWebUIItem.Enabled = isRunning;

        var cachedUrl = _serviceStatusService.CachedWebUIUrl;
        if (!isRunning || string.IsNullOrEmpty(cachedUrl))
        {
            openWebUIItem.Text = "Open Web UI";
        }
        else
        {
            openWebUIItem.Text = $"Open Web UI ({cachedUrl})";
        }
    }

    private void OnTrayIconDoubleClick(object? sender, EventArgs e)
    {
        OnOpenWebUI(sender, e);
    }

    private void OnBalloonTipClicked(object? sender, EventArgs e)
    {
        OnOpenWebUI(sender, e);
    }

    private void OnStartService(object? sender, EventArgs e)
    {
        try
        {
            _serviceStatusService.StartService();
            ShowBalloonTip("Service Started", "Papercut SMTP Service is now running.", ToolTipIcon.Info);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 5) // Access Denied
        {
            MessageBox.Show(
                "This application must be run as Administrator to control the Papercut SMTP Service.\n\n" +
                "Please close this application and restart it by right-clicking and selecting 'Run as administrator'.",
                "Administrator Privileges Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (InvalidOperationException)
        {
            MessageBox.Show(
                "The Papercut SMTP Service could not be found or accessed.\n\n" +
                "Please ensure the service is installed.",
                "Service Not Found",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to start service: {ex.Message}\n\n" +
                "Make sure you have administrator privileges and the service is installed.",
                "Error Starting Service",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void OnStopService(object? sender, EventArgs e)
    {
        try
        {
            _serviceStatusService.StopService();
            ShowBalloonTip("Service Stopped", "Papercut SMTP Service has been stopped.", ToolTipIcon.Info);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 5) // Access Denied
        {
            MessageBox.Show(
                "This application must be run as Administrator to control the Papercut SMTP Service.\n\n" +
                "Please close this application and restart it by right-clicking and selecting 'Run as administrator'.",
                "Administrator Privileges Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (InvalidOperationException)
        {
            MessageBox.Show(
                "The Papercut SMTP Service could not be found or accessed.\n\n" +
                "Please ensure the service is installed.",
                "Service Not Found",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to stop service: {ex.Message}\n\n" +
                "Make sure you have administrator privileges and the service is installed.",
                "Error Stopping Service",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void OnRestartService(object? sender, EventArgs e)
    {
        try
        {
            _serviceStatusService.RestartService();
            ShowBalloonTip("Service Restarted", "Papercut SMTP Service has been restarted.", ToolTipIcon.Info);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 5) // Access Denied
        {
            MessageBox.Show(
                "This application must be run as Administrator to control the Papercut SMTP Service.\n\n" +
                "Please close this application and restart it by right-clicking and selecting 'Run as administrator'.",
                "Administrator Privileges Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (InvalidOperationException)
        {
            MessageBox.Show(
                "The Papercut SMTP Service could not be found or accessed.\n\n" +
                "Please ensure the service is installed.",
                "Service Not Found",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to restart service: {ex.Message}\n\n" +
                "Make sure you have administrator privileges and the service is installed.",
                "Error Restarting Service",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private async void OnOpenWebUI(object? sender, EventArgs e)
    {
        // Check if service is running before attempting to open web UI
        if (!_serviceStatusService.IsServiceInstalled)
        {
            MessageBox.Show(
                "The Papercut SMTP Service is not installed.\n\nPlease install the service first.",
                "Service Not Installed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (_serviceStatusService.CurrentStatus != ServiceControllerStatus.Running)
        {
            MessageBox.Show(
                "The Papercut SMTP Service is not running.\n\nPlease start the service first.",
                "Service Not Running",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var webUrl = await _serviceStatusService.GetWebUIUrlAsync();

            Process.Start(new ProcessStartInfo
            {
                FileName = webUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to open web UI: {ex.Message}\n\nMake sure the service is running and accessible.",
                "Error Opening Web UI",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void OnViewLogs(object? sender, EventArgs e)
    {
        try
        {
            var logsPath = _loggingPathConfigurator.DefaultSavePath;

            if (!Directory.Exists(logsPath))
            {
                Directory.CreateDirectory(logsPath);
            }

            Log.Information("Opening logs folder at {LogsPath}", logsPath);

            Process.Start(new ProcessStartInfo
            {
                FileName = logsPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open logs folder");
            MessageBox.Show(
                $"Failed to open logs folder: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void OnToggleRunOnStartup(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem menuItem)
            return;

        try
        {
            bool success = _appRunOnStartupService.SetRunOnStartup(menuItem.Checked);

            if (success)
            {
                Log.Information("Run on startup set to {Enabled}", menuItem.Checked);
            }
            else
            {
                // Revert the checkbox if the operation failed
                menuItem.Checked = !menuItem.Checked;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to toggle run on startup");
            // Revert the checkbox
            menuItem.Checked = !menuItem.Checked;
        }
    }

    private void OnToggleNotifications(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem menuItem)
            return;

        try
        {
            _notificationService.NotificationsEnabled = menuItem.Checked;
            Log.Information("Notifications {Status}", menuItem.Checked ? "enabled" : "disabled");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to toggle notifications");
            // Revert the checkbox
            menuItem.Checked = !menuItem.Checked;
        }
    }

    private void OnExit(object? sender, EventArgs e)
    {
        Log.Information("Exiting Service Tray Manager");
        Application.Exit();
    }

    private void ShowBalloonTip(string title, string text, ToolTipIcon icon)
    {
        _notifyIcon.ShowBalloonTip(3000, title, text, icon);
    }

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    private static void Register(ContainerBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.RegisterType<ServiceTrayCoordinator>().AsSelf().SingleInstance();
    }

    #endregion
}
