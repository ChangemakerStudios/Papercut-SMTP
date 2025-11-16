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


using System.Security;

using Autofac;

using Microsoft.Win32;

using Papercut.Core.Domain.Application;

namespace Papercut.Service.TrayNotification;

/// <summary>
/// Manages the "Run at Startup" functionality for the tray notification app
/// </summary>
public class AppRunOnStartupService(IAppMeta appMeta, ILogger logger)
{
    private const string AppStartupKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// Checks if the app is configured to run at startup
    /// </summary>
    public bool IsRunOnStartupEnabled()
    {
        try
        {
            using var registryKey = Registry.CurrentUser.OpenSubKey(AppStartupKey, false);

            if (registryKey == null)
            {
                return false;
            }

            var executablePath = Environment.ProcessPath;
            var currentValue = registryKey.GetValue(appMeta.AppName, null) as string;

            return currentValue == executablePath;
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to check run on startup status");
            return false;
        }
    }

    /// <summary>
    /// Enables or disables run at startup
    /// </summary>
    public bool SetRunOnStartup(bool enabled)
    {
        try
        {
            using var registryKey = Registry.CurrentUser.OpenSubKey(AppStartupKey, true);

            if (registryKey == null)
            {
                logger.Error("Failed to open registry key {AppStartupKey}", AppStartupKey);
                return false;
            }

            var applicationName = appMeta.AppName;
            var executablePath = Environment.ProcessPath;

            if (string.IsNullOrEmpty(executablePath))
            {
                logger.Error("Failed to get executable path");
                return false;
            }

            // Check current state
            var currentValue = registryKey.GetValue(applicationName, null) as string;
            bool isCurrentlyEnabled = currentValue == executablePath;

            if (enabled && !isCurrentlyEnabled)
            {
                // Enable run at startup
                if (!File.Exists(executablePath))
                {
                    logger.Error(
                        "Executable doesn't exist at {ExecutablePath} -- Run at Startup Disabled",
                        executablePath);
                    return false;
                }

                logger.Information(
                    "Setting AppStartup Registry {Key} to {ExecutablePath}",
                    $"{AppStartupKey}\\{applicationName}",
                    executablePath);

                registryKey.SetValue(applicationName, executablePath);
                return true;
            }
            else if (!enabled && isCurrentlyEnabled)
            {
                // Disable run at startup
                logger.Information(
                    "Deleting AppStartup Registry {Key}",
                    $"{AppStartupKey}\\{applicationName}");

                registryKey.DeleteValue(applicationName, false);
                return true;
            }

            // Already in desired state
            return true;
        }
        catch (SecurityException ex)
        {
            logger.Error(ex, "Security exception when trying to modify registry");
            MessageBox.Show(
                "Failed to modify startup settings due to lack of permission.\n\n" +
                "The application is already running with administrator privileges, but registry access was denied.",
                "Permission Denied",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to set run on startup");
            MessageBox.Show(
                $"Failed to modify startup settings: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }
    }

    #region Begin Static Container Registrations

    [UsedImplicitly]
    private static void Register(ContainerBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.RegisterType<AppRunOnStartupService>().AsSelf().SingleInstance();
    }

    #endregion
}
