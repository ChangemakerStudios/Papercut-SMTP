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


using System.ServiceProcess;

using Autofac;

namespace Papercut.Service.TrayNotification;

/// <summary>
/// Handles all service status monitoring and control operations
/// </summary>
public class ServiceStatusService(
    PapercutServiceControllerProvider serviceControllerProvider,
    ServiceCommunicator serviceCommunicator)
{
    private ServiceControllerStatus? _lastKnownStatus;

    public ServiceControllerStatus? CurrentStatus => _lastKnownStatus;

    public bool IsServiceInstalled { get; private set; } = true;

    public event EventHandler<ServiceControllerStatus>? StatusChanged;

    /// <summary>
    /// Updates the current service status
    /// </summary>
    public void UpdateStatus()
    {
        try
        {
            var status = serviceControllerProvider.GetStatus();

            if (_lastKnownStatus != status)
            {
                _lastKnownStatus = status;
                IsServiceInstalled = true;
                StatusChanged?.Invoke(this, status);
            }
        }
        catch (InvalidOperationException)
        {
            // Service not found/installed
            if (_lastKnownStatus != null || IsServiceInstalled)
            {
                _lastKnownStatus = null;
                IsServiceInstalled = false;
                StatusChanged?.Invoke(this, default);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to check service status");
        }
    }

    /// <summary>
    /// Starts the service
    /// </summary>
    public void StartService()
    {
        var status = serviceControllerProvider.GetStatus();

        if (status == ServiceControllerStatus.Stopped)
        {
            serviceControllerProvider.Start();
            serviceCommunicator.InvalidateCache();
            UpdateStatus();
        }
    }

    /// <summary>
    /// Stops the service
    /// </summary>
    public void StopService()
    {
        var status = serviceControllerProvider.GetStatus();

        if (status == ServiceControllerStatus.Running)
        {
            serviceControllerProvider.Stop();
            UpdateStatus();
        }
    }

    /// <summary>
    /// Restarts the service
    /// </summary>
    public void RestartService()
    {
        var status = serviceControllerProvider.GetStatus();

        if (status == ServiceControllerStatus.Running)
        {
            serviceControllerProvider.Restart();
            serviceCommunicator.InvalidateCache();
            UpdateStatus();
        }
    }

    /// <summary>
    /// Gets the web UI URL from the service via IPComm
    /// </summary>
    public async Task<string> GetWebUIUrlAsync()
    {
        return await serviceCommunicator.GetWebUIUrlAsync();
    }

    /// <summary>
    /// Gets the cached web UI URL without making an async call
    /// </summary>
    public string? CachedWebUIUrl => serviceCommunicator.CachedWebUrl;

    /// <summary>
    /// Gets a display-friendly status text
    /// </summary>
    public string GetStatusText()
    {
        if (!IsServiceInstalled)
        {
            return "Not Installed";
        }

        return _lastKnownStatus switch
        {
            ServiceControllerStatus.Running => "Running",
            ServiceControllerStatus.Stopped => "Stopped",
            ServiceControllerStatus.StartPending => "Starting...",
            ServiceControllerStatus.StopPending => "Stopping...",
            null => "Unknown",
            _ => _lastKnownStatus.ToString() ?? "Unknown"
        };
    }

    /// <summary>
    /// Checks if the service can be started
    /// </summary>
    public bool CanStart()
    {
        return IsServiceInstalled && _lastKnownStatus == ServiceControllerStatus.Stopped;
    }

    /// <summary>
    /// Checks if the service can be stopped
    /// </summary>
    public bool CanStop()
    {
        return IsServiceInstalled && _lastKnownStatus == ServiceControllerStatus.Running;
    }

    /// <summary>
    /// Checks if the service can be restarted
    /// </summary>
    public bool CanRestart()
    {
        return IsServiceInstalled && _lastKnownStatus == ServiceControllerStatus.Running;
    }

    #region Begin Static Container Registrations

    [UsedImplicitly]
    private static void Register(ContainerBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.RegisterType<ServiceStatusService>().AsSelf().SingleInstance();
    }

    #endregion
}
