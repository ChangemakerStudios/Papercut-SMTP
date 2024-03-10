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


using System;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using ElectronNET.API;
using ElectronNET.API.Entities;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.PlatformAbstractions;

namespace Papercut.Desktop
{
    public class PapercutHybridSupport : IHostedService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;

        public PapercutHybridSupport(IHostApplicationLifetime hostApplicationLifetime)
        {
            this._hostApplicationLifetime = hostApplicationLifetime;
            //settingsStore.Set("HttpPort", BridgeSettings.WebPort);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Electron.App.WillQuit += (q) =>
            {
                this._hostApplicationLifetime.StopApplication();
                return Task.CompletedTask;
            };

            await Electron.WindowManager.CreateWindowAsync(
                new BrowserWindowOptions
                {
                    Width = 1152,
                    Height = 864,
                    Show = true,
                    BackgroundColor = "#f5f6f8",
                    Title = "Papercut SMTP",
                    Icon = WindowIcon()
                });

            //var socket = GetElectronSocket();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public static void Quit()
        {
            var socket = GetElectronSocket();

            //socket.On(Socket.EVENT_CONNECT, () =>
            //{
            //    Electron.App.Quit();
                
            //    Thread.Sleep(100);
            //    Environment.Exit(-1);
            //});
            
            // Wait at most 2 seconds
            //Thread.Sleep(2000);
            //Environment.Exit(-1);
        }

        static Socket GetElectronSocket()
        {
            var socketProp = Electron.WindowManager
                .GetType().Assembly
                .GetType("ElectronNET.API.BridgeConnector")
                .GetProperty("Socket", BindingFlags.Static | BindingFlags.Public);
            
            return  socketProp.GetValue(null) as Socket;
        }

        static void QuitOnConnectionProblems(Socket socket, string eventType)
        {
            void Handler()
            {
                Console.Error.WriteLine("Papercut backend process is exiting because of connection with frontend Electron process is error. Event type: " + eventType);
                Service.Program.Shutdown();
            }

            //socket.On(eventType, Handler);
        }

        static string WindowIcon()
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            return Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "icons",
                isWindows ? "Papercut-icon.ico" : "Papercut-icon.png");
        }

        public Task Start(CancellationToken token)
        {
            return Task.CompletedTask;

            //QuitOnConnectionProblems(socket, Socket.EVENT_CONNECT_ERROR);
            //QuitOnConnectionProblems(socket, Socket.EVENT_RECONNECT_ERROR);
        }
    }
}