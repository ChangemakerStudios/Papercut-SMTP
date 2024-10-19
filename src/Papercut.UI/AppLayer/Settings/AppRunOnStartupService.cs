// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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

using Papercut.Common.Domain;
using Papercut.Common.Extensions;
using Papercut.Domain.Application;
using Papercut.Domain.Events;
using Papercut.Domain.UiCommands;

namespace Papercut.AppLayer.Settings;

public class AppRunOnStartupService(ILogger logger, IUiCommandHub uiCommandHub) : IEventHandler<SettingsUpdatedEvent>
{
    const string AppStartupKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public Task HandleAsync(SettingsUpdatedEvent @event, CancellationToken token)
    {
        // check if the setting changed
        if (@event.PreviousSettings.RunOnStartup == @event.NewSettings.RunOnStartup)
            return Task.CompletedTask;

        try
        {
            var registryKey = Registry.CurrentUser.OpenSubKey(AppStartupKey, true);

            if (registryKey == null)
            {
                logger.Error("Failure opening registry key {AppStartupKey}", AppStartupKey);
                return Task.CompletedTask;
            }

            var applicationName = PapercutAppConstants.Name;
            var executablePath = PapercutAppConstants.ExecutablePath;

            // is key currently set to this app executable?
            bool runOnStartup = registryKey.GetValue(applicationName, null)
                .ToType<string>() == executablePath;

            if (Properties.Settings.Default.RunOnStartup && !runOnStartup)
            {
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

        return Task.CompletedTask;
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