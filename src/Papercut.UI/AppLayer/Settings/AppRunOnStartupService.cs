// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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

namespace Papercut.AppLayer.Settings
{
    using System;
    using System.Security;
    using System.Threading;
    using System.Threading.Tasks;

    using Autofac;

    using Microsoft.Win32;

    using Papercut.Common.Domain;
    using Papercut.Common.Extensions;
    using Papercut.Core.Annotations;
    using Papercut.Domain.Events;
    using Papercut.Domain.UiCommands;
    using Papercut.Properties;

    using Serilog;

    public class AppRunOnStartupService : IEventHandler<SettingsUpdatedEvent>
    {
        const string AppStartupKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        readonly ILogger _logger;

        private readonly IUiCommandHub _uiCommandHub;

        public AppRunOnStartupService(ILogger logger, IUiCommandHub uiCommandHub)
        {
            this._logger = logger;
            this._uiCommandHub = uiCommandHub;
        }

        public Task HandleAsync(SettingsUpdatedEvent @event, CancellationToken token)
        {
            // check if the setting changed
            if (@event.PreviousSettings.RunOnStartup == @event.NewSettings.RunOnStartup)
                return Task.CompletedTask;

            try
            {
                RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(AppStartupKey, true);

                if (registryKey == null)
                {
                    this._logger.Error("Failure opening registry key {AppStartupKey}", AppStartupKey);
                    return Task.CompletedTask;
                }

                // is key currently set to this app executable?
                bool runOnStartup = registryKey.GetValue(App.GlobalName, null)
                                        .ToType<string>() == App.ExecutablePath;

                if (Settings.Default.RunOnStartup && !runOnStartup)
                {
                    // turn on..
                    this._logger.Information(
                        "Setting AppStartup Registry {Key} to Run Papercut at {ExecutablePath}",
                        $"{AppStartupKey}\\{App.GlobalName}",
                        App.ExecutablePath);

                    registryKey.SetValue(App.GlobalName, App.ExecutablePath);
                }
                else if (!Settings.Default.RunOnStartup && runOnStartup)
                {
                    // turn off...
                    this._logger.Information(
                        "Attempting to Delete AppStartup Registry {Key}",
                        $"{AppStartupKey}\\{App.GlobalName}");

                    registryKey.DeleteValue(App.GlobalName, false);
                }
            }
            catch (SecurityException ex)
            {
                this._logger.Error(ex, "Error Opening Registry for App Startup Service");

                this._uiCommandHub.ShowMessage(
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
        static void Register([NotNull] ContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.RegisterType<AppRunOnStartupService>().AsImplementedInterfaces()
                .InstancePerLifetimeScope();
        }

        #endregion
    }
}