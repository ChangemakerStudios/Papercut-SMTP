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

using Microsoft.Win32;

using Papercut.Domain.Application;
using Papercut.Domain.UiCommands;

namespace Papercut.AppLayer.Settings;

public class AppRunOnStartupService(ILogger logger, IUiCommandHub uiCommandHub) : IAppLifecycleStarted, IEventHandler<SettingsUpdatedEvent>
{
    const string AppStartupKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public Task OnStartedAsync()
    {
        if (Properties.Settings.Default.RunOnStartup)
        {
            // check if we need to migrate....
            bool needsMigration = false;
            try
            {
                using var registryKey = Registry.CurrentUser.OpenSubKey(AppStartupKey, true);

                object? legacyAppKey = registryKey?.GetValue(PapercutAppConstants.LegacyName, null);

                if (registryKey != null && legacyAppKey != null)
                {
                    needsMigration = true;

                    logger.Information(
                        "Migrating App Run on Startup Registry {LegacyKey} to {NewKey}",
                        $"{AppStartupKey}\\{PapercutAppConstants.LegacyName}",
                        $"{AppStartupKey}\\{PapercutAppConstants.Name}");

                    registryKey.DeleteValue(PapercutAppConstants.LegacyName, false);
                }
            }
            catch (Exception ex)
            {
                logger.Debug(ex, "Failure deleting legacy app key");
            }

            if (needsMigration)
            {
                UpdateRunAtStartup();
            }
        }

        return Task.CompletedTask;
    }

    public Task HandleAsync(SettingsUpdatedEvent @event, CancellationToken token)
    {
        // check if the setting changed
        if (@event.PreviousSettings.RunOnStartup == @event.NewSettings.RunOnStartup)
            return Task.CompletedTask;

        UpdateRunAtStartup();

        return Task.CompletedTask;
    }

    private bool UpdateRunAtStartup()
    {
        try
        {
            using var registryKey = Registry.CurrentUser.OpenSubKey(AppStartupKey, true);

            if (registryKey == null)
            {
                logger.Error("Failure opening registry key {AppStartupKey}", AppStartupKey);
                return true;
            }

            var applicationName = PapercutAppConstants.Name;
            var executablePath = PapercutAppConstants.ExecutablePath;

            // is key currently set to this app executable?
            bool runOnStartup = registryKey.GetValue(applicationName, null)
                .ToType<string>() == executablePath;

            if (Properties.Settings.Default.RunOnStartup && !runOnStartup)
            {
                if (!File.Exists(executablePath))
                {
                    logger.Error(
                        "App Startup Failure: {ExecutablePath} for Papercut Doesn't Exist -- Run at Startup Disabled",
                        executablePath);

                    Properties.Settings.Default.RunOnStartup = false;

                    return false;
                }

                // turn on...
                logger.Information(
                    "Setting AppStartup Registry {Key} to Run Papercut at {ExecutablePath}",
                    $"{AppStartupKey}\\{applicationName}",
                    executablePath);

                registryKey.SetValue(applicationName, executablePath);
            }
            else if (!Properties.Settings.Default.RunOnStartup && runOnStartup)
            {
                // turn off...
                logger.Information(
                    "Attempting to Delete AppStartup Registry {Key}",
                    $"{AppStartupKey}\\{applicationName}");

                registryKey.DeleteValue(applicationName, false);
            }
        }
        catch (SecurityException ex)
        {
            logger.Error(ex, "Error Opening Registry for App Startup Service");

            uiCommandHub.ShowMessage(
                "Failed to set Papercut to load at startup due to lack of permission. To fix, exit and run Papercut again with elevated (Admin) permissions.",
                "Failed");
        }

        return false;
    }

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    static void Register(ContainerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RegisterType<AppRunOnStartupService>().AsImplementedInterfaces()
            .InstancePerLifetimeScope();
    }

    #endregion
}