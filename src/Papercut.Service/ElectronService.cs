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


using System.Runtime.InteropServices;

using ElectronNET.API;
using ElectronNET.API.Entities;

using Microsoft.Extensions.Hosting;

namespace Papercut.Service;

public class ElectronService : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Information("Running Electron Create Window Service");

        Electron.App.WillQuit += (q) =>
        {
            Program.Shutdown();
            return Task.CompletedTask;
        };

        var browserWindow = await Electron.WindowManager.CreateWindowAsync(
            new BrowserWindowOptions
            {
                Width = 1152,
                Height = 864,
                Show = false,
                BackgroundColor = "#f5f6f8",
                Title = "Papercut SMTP",
                Icon = WindowIcon()
            });

        await browserWindow.WebContents.Session.ClearCacheAsync();

        browserWindow.OnReadyToShow += () =>
        {
            browserWindow.Show();
        };

        var menu = new MenuItem[]
                   {

                       new()
                       {
                           Type = MenuType.normal,
                           Label = "MenuItem",
                           Click = () =>
                           {
                               Electron.Notification.Show(
                                   new NotificationOptions(
                                       "Dock MenuItem Click",
                                       "A menu item added to the Dock was selected;"));
                           },
                       },
                       new()
                       {
                           Type = MenuType.submenu,
                           Label = "SubMenu",
                           Submenu =
                           [
                               new MenuItem
                               {
                                   Type = MenuType.normal,
                                   Label = "Sub MenuItem",
                                   Click = () =>
                                   {
                                       Electron.Notification.Show(
                                           new NotificationOptions(
                                               "Dock Sub MenuItem Click",
                                               "A menu item added to the Dock was selected;"));
                                   },
                               }
                           ]
                       }
                   };

        Electron.Menu.SetApplicationMenu(menu);

        await Electron.App.On("activate", (obj) =>
        {
            // obj should be a boolean that represents where there are active windows or not.
            var hasWindows = (bool)obj;

            Electron.Notification.Show(
                new NotificationOptions("Activate", $"activate event has been captured. Active windows = {hasWindows}")
                {
                    Silent = false,
                });
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Electron.App.Exit();

        return Task.CompletedTask;
    }

    static string WindowIcon()
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        return Path.Combine(AppContext.BaseDirectory, "icons",
            isWindows ? "Papercut-icon.ico" : "Papercut-icon.png");
    }
}