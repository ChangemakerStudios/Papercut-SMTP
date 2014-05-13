// /*  
//  * Papercut
//  *
//  *  Copyright © 2008 - 2012 Ken Robertson
//  *  Copyright © 2013 - 2014 Jaben Cargman
//  *  
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *  
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *  
//  *  Unless required by applicable law or agreed to in writing, software
//  *  distributed under the License is distributed on an "AS IS" BASIS,
//  *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *  See the License for the specific language governing permissions and
//  *  limitations under the License.
//  *  
//  */


namespace Papercut.Services
{
    using System;
    using System.Security;
    using System.Windows;

    using Microsoft.Win32;

    using Papercut.Core.Events;
    using Papercut.Core.Helper;
    using Papercut.Events;
    using Papercut.Helpers;
    using Papercut.Properties;

    using Serilog;

    public class AppStartupService : IHandleEvent<SettingsUpdatedEvent>
    {
        const string AppStartupKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public ILogger Logger { get; set; }

        public IPublishEvent PublishEvent { get; set; }

        public AppStartupService(ILogger logger, IPublishEvent publishEvent)
        {
            Logger = logger;
            PublishEvent = publishEvent;
        }

        public void Handle(SettingsUpdatedEvent @event)
        {
            try
            {
                var registryKey = Registry.CurrentUser.OpenSubKey(AppStartupKey, true);

                // is key currenctly set to this app executable?
                var runOnStartup = registryKey.GetValue(App.GlobalName, null).ToType<string>() == App.ExecutablePath;

                if (Settings.Default.RunOnStartup && !runOnStartup)
                {
                    // turn on..
                    Logger.Information(
                        "Setting AppStartup Registry {Key} to Run Papercut at {ExecutablePath}",
                        string.Format("{0}\\{1}", AppStartupKey, App.GlobalName),
                        App.ExecutablePath);

                    registryKey.SetValue(App.GlobalName, App.ExecutablePath);
                }
                else if (!Settings.Default.RunOnStartup && runOnStartup)
                {
                    // turn off...
                    Logger.Information(
                        "Attempting to Delete AppStartup Registry {Key}",
                        string.Format("{0}\\{1}", AppStartupKey, App.GlobalName));

                    registryKey.DeleteValue(App.GlobalName, false);
                }
            }
            catch (SecurityException ex)
            {
                Logger.Error(ex, "Error Opening Registry for App Startup Service");
                PublishEvent.Publish(
                    new ShowMessageEvent(
                        "Failed to set Papercut to load at startup due to lack of permission. To fix, exit and run Papercut again with elevated (Admin) permissions.",
                        "Failed"));
            }
        }
    }
}