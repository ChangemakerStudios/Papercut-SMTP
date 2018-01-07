
// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2017 Jaben Cargman
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

using System;
using Autofac;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.Hosting;
using Papercut.Core.Domain.Settings;
using Papercut.Service;

namespace Papercut.Desktop
{
    public class Program
    {
        static int Main(string[] args)
        {
            WebServerReadyEvent.Register(ev =>
            {
                if (HybridSupport.IsElectronActive)
                {
                    BootstrapElectron();
                }
            });
            
            
            var appTask = Papercut.Service.Program.Startup(args, appContainer =>
            {
                new WebHostBuilder().UseElectron(args);
                if (HybridSupport.IsElectronActive)
                {
                    appContainer.Resolve<ISettingStore>().Set("HttpPort", BridgeSettings.WebPort);
                }
                else
                {
                    Console.Error.WriteLine("Electron context is not detected. The application will run in console mode.");
                }
            });

            appTask.Wait();
            return appTask.Result;
        }
        
        static async void BootstrapElectron()
        {
            Electron.App.WillQuit += Papercut.Service.Program.Shutdown;
            
            var browserWindow = await Electron.WindowManager.CreateWindowAsync(new BrowserWindowOptions
            {
                Width = 1152,
                Height = 864,
                Show = false
            });
            
            browserWindow.SetTitle("Papercut");
            browserWindow.OnReadyToShow += () => browserWindow.Show();
        }
    }
}