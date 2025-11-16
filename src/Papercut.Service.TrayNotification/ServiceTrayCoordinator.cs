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

namespace Papercut.Service.TrayNotification;

public class ServiceTrayCoordinator : IDisposable
{
    private const string ServiceName = "Papercut.SMTP.Service";

    private readonly LoggingPathConfigurator _loggingPathConfigurator;

    private readonly NotifyIcon _notifyIcon;

    private readonly ServiceCommunicator _serviceCommunicator;

    private readonly System.Windows.Forms.Timer _statusUpdateTimer;

    private ServiceControllerStatus? _lastStatus;

    public ServiceTrayCoordinator(LoggingPathConfigurator loggingPathConfigurator, ServiceCommunicator serviceCommunicator)
    {
        _loggingPathConfigurator = loggingPathConfigurator;
        _serviceCommunicator = serviceCommunicator;

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Papercut SMTP Service Manager",
            Visible = true
        };

        _notifyIcon.DoubleClick += OnTrayIconDoubleClick;
        _notifyIcon.ContextMenuStrip = CreateContextMenu();

        // Update service status every 2 seconds
        _statusUpdateTimer = new System.Windows.Forms.Timer
        {
            Interval = 2000
        };
        _statusUpdateTimer.Tick += (_, _) => UpdateServiceStatus();
        _statusUpdateTimer.Start();

        // Initial status update
        UpdateServiceStatus();

        Log.Information("Service Tray Coordinator initialized");
    }

    public void Dispose()
    {
        _statusUpdateTimer?.Stop();
        _statusUpdateTimer?.Dispose();
        _notifyIcon?.Dispose();
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

        menu.Items.Add("Open Web UI", null, OnOpenWebUI);
        menu.Items.Add("View Logs Folder", null, OnViewLogs);

        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add("Exit", null, OnExit);

        // Update menu state before showing
        menu.Opening += (_, _) => UpdateMenuState(menu);

        return menu;
    }

    private void UpdateServiceStatus()
    {
        try
        {
            using var controller = new ServiceController(ServiceName);
            var status = controller.Status;

            if (_lastStatus != status)
            {
                _lastStatus = status;
                UpdateTrayIcon(status);
                Log.Debug("Service status changed to {Status}", status);
            }
        }
        catch (InvalidOperationException)
        {
            // Service not found/installed
            if (_lastStatus != null)
            {
                _lastStatus = null;
                _notifyIcon.Text = "Papercut SMTP Service (Not Installed)";
                _notifyIcon.Icon = LoadIcon();
                Log.Warning("Service '{ServiceName}' not found or not installed", ServiceName);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to check service status");
        }
    }

    private void UpdateTrayIcon(ServiceControllerStatus status)
    {
        var statusText = status switch
        {
            ServiceControllerStatus.Running => "Running",
            ServiceControllerStatus.Stopped => "Stopped",
            ServiceControllerStatus.StartPending => "Starting...",
            ServiceControllerStatus.StopPending => "Stopping...",
            _ => status.ToString()
        };

        _notifyIcon.Text = $"Papercut SMTP Service ({statusText})";

        // Could add different icons or overlays based on status in the future
        _notifyIcon.Icon = LoadIcon();
    }

    private void UpdateMenuState(ContextMenuStrip menu)
    {
        if (menu.Items["statusLabel"] is not ToolStripLabel statusLabel || menu.Items["startService"] is not ToolStripMenuItem startItem
                                                                        || menu.Items["stopService"] is not ToolStripMenuItem stopItem
                                                                        || menu.Items["restartService"] is not ToolStripMenuItem restartItem)
            return;

        try
        {
            using var controller = new ServiceController(ServiceName);
            var status = controller.Status;

            statusLabel.Text = status switch
            {
                ServiceControllerStatus.Running => "● Service Running",
                ServiceControllerStatus.Stopped => "○ Service Stopped",
                ServiceControllerStatus.StartPending => "◐ Service Starting...",
                ServiceControllerStatus.StopPending => "◑ Service Stopping...",
                _ => $"◌ Service {status}"
            };

            startItem.Enabled = status == ServiceControllerStatus.Stopped;
            stopItem.Enabled = status == ServiceControllerStatus.Running;
            restartItem.Enabled = status == ServiceControllerStatus.Running;
        }
        catch (InvalidOperationException)
        {
            statusLabel.Text = "✗ Service Not Installed";
            startItem.Enabled = false;
            stopItem.Enabled = false;
            restartItem.Enabled = false;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to update menu state");
            statusLabel.Text = "? Service Status Unknown";
        }
    }

    private void OnTrayIconDoubleClick(object? sender, EventArgs e)
    {
        OnOpenWebUI(sender, e);
    }

    private void OnStartService(object? sender, EventArgs e)
    {
        try
        {
            Log.Information("Starting service {ServiceName}", ServiceName);
            using var controller = new ServiceController(ServiceName);

            if (controller.Status == ServiceControllerStatus.Stopped)
            {
                controller.Start();
                controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                _serviceCommunicator.InvalidateCache(); // Re-detect web URL after start

                ShowBalloonTip("Service Started", "Papercut SMTP Service is now running.", ToolTipIcon.Info);
            }
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
                $"The service '{ServiceName}' could not be found or accessed.\n\n" +
                "Please ensure the Papercut SMTP Service is installed.",
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
            Log.Information("Stopping service {ServiceName}", ServiceName);
            using var controller = new ServiceController(ServiceName);

            if (controller.Status == ServiceControllerStatus.Running)
            {
                controller.Stop();
                controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                ShowBalloonTip("Service Stopped", "Papercut SMTP Service has been stopped.", ToolTipIcon.Info);
            }
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
                $"The service '{ServiceName}' could not be found or accessed.\n\n" +
                "Please ensure the Papercut SMTP Service is installed.",
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
            Log.Information("Restarting service {ServiceName}", ServiceName);
            using var controller = new ServiceController(ServiceName);

            if (controller.Status == ServiceControllerStatus.Running)
            {
                controller.Stop();
                controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                controller.Start();
                controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                _serviceCommunicator.InvalidateCache(); // Re-detect web URL after restart

                ShowBalloonTip("Service Restarted", "Papercut SMTP Service has been restarted.", ToolTipIcon.Info);
            }
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
                $"The service '{ServiceName}' could not be found or accessed.\n\n" +
                "Please ensure the Papercut SMTP Service is installed.",
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
        try
        {
            var webUrl = await _serviceCommunicator.GetWebUIUrlAsync();
            Log.Information("Opening web UI at {WebUIUrl}", webUrl);

            Process.Start(new ProcessStartInfo
            {
                FileName = webUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open web UI");
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
