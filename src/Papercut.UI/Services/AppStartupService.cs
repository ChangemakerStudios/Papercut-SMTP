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

namespace Papercut.Services
{
    using System.Security;
    using System.Threading.Tasks;

    using Microsoft.Win32;

    using Papercut.Common.Domain;
    using Papercut.Common.Extensions;
    using Papercut.Events;
    using Papercut.Properties;

    using Serilog;

    public class AppStartupService : IEventHandler<SettingsUpdatedEvent>
    {
        const string AppStartupKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        readonly ILogger _logger;

        readonly IMessageBus _messageBus;

        public AppStartupService(ILogger logger, IMessageBus messageBus)
        {
            _logger = logger;
            this._messageBus = messageBus;
        }

        public void Handle(SettingsUpdatedEvent @event)
        {
            // check if the setting changed
            if (@event.PreviousSettings.RunOnStartup == @event.NewSettings.RunOnStartup)
                return;

            try
            {
                RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(AppStartupKey, true);

                if (registryKey == null)
                {
                    this._logger.Error("Failure opening registry key {AppStartupKey}", AppStartupKey);
                    return;
                }

                // is key currently set to this app executable?
                bool runOnStartup = registryKey.GetValue(App.GlobalName, null)
                                        .ToType<string>() == App.ExecutablePath;

                if (Settings.Default.RunOnStartup && !runOnStartup)
                {
                    // turn on..
                    _logger.Information(
                        "Setting AppStartup Registry {Key} to Run Papercut at {ExecutablePath}",
                        $"{AppStartupKey}\\{App.GlobalName}",
                        App.ExecutablePath);

                    registryKey.SetValue(App.GlobalName, App.ExecutablePath);
                }
                else if (!Settings.Default.RunOnStartup && runOnStartup)
                {
                    // turn off...
                    _logger.Information(
                        "Attempting to Delete AppStartup Registry {Key}",
                        $"{AppStartupKey}\\{App.GlobalName}");

                    registryKey.DeleteValue(App.GlobalName, false);
                }
            }
            catch (SecurityException ex)
            {
                _logger.Error(ex, "Error Opening Registry for App Startup Service");

                this._messageBus.Publish(
                    new ShowMessageEvent(
                        "Failed to set Papercut to load at startup due to lack of permission. To fix, exit and run Papercut again with elevated (Admin) permissions.",
                        "Failed"));
            }
        }
    }
}