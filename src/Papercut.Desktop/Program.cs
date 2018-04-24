
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
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.PlatformAbstractions;
using Papercut.Core.Domain.Settings;
using Papercut.Service;
using Quobject.SocketIoClientDotNet.Client;

namespace Papercut.Desktop
{
    public class Program
    {
        static int Main(string[] args)
        {
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DEBUG_PAPAERCUT")))
            {
                Console.WriteLine("Waiting 30s for the debugger to attach...");
                Thread.Sleep(30 * 1000);
            }
            
            
            
            
            Task<int> appTask;
            new WebHostBuilder().UseElectron(args);
            if (HybridSupport.IsElectronActive)
            {
                // Quit if the process is not launched by the customized main.js
                var bootstraper = Environment.GetEnvironmentVariable("PAPERCUT_BOOTSTRAPER");
                if (string.IsNullOrWhiteSpace(bootstraper))
                {
                    Console.WriteLine("Papercut.Desktop: skiping this running attempt.");
                    PapercutHybridSupport.Quit();
                }
                
                            
                WebServerReadyEvent.Register(ev =>
                {
                    PapercutHybridSupport.Bootstrap();
                });
                
                appTask = Papercut.Service.Program.Startup(args, appContainer =>
                {
                    appContainer.Resolve<ISettingStore>().Set("HttpPort", BridgeSettings.WebPort);
                });
            }
            else
            {
                Console.Error.WriteLine("Electron context is not detected. The application will run in console mode.");
                appTask = Papercut.Service.Program.Startup(args);
            }

            appTask.Wait();
            return appTask.Result;
        }
    }
}