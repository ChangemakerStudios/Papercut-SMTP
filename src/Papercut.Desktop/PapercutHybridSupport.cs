using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.Extensions.PlatformAbstractions;
using Quobject.SocketIoClientDotNet.Client;

namespace Papercut.Desktop
{
    using System.Threading.Tasks;

    using Papercut.Core.Domain.Settings;
    using Papercut.Core.Infrastructure.Lifecycle;

    public class PapercutHybridSupport : IStartupService
    {
        public PapercutHybridSupport(ISettingStore settingsStore)
        {
            settingsStore.Set("HttpPort", BridgeSettings.WebPort);
        }

        public static void Quit()
        {
            var socket = GetElectronSocket();
            socket.On(Socket.EVENT_CONNECT, () =>
            {
                Electron.App.Quit();
                
                Thread.Sleep(100);
                Environment.Exit(-1);
            });
            
            // Wait at most 2 seconds
            Thread.Sleep(2000);
            Environment.Exit(-1);
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
                Papercut.Service.Program.Shutdown();
            }

            socket.On(eventType, Handler);
        }

        static string WindowIcon()
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            return Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "icons",
                isWindows ? "Papercut-icon.ico" : "Papercut-icon.png");
        }

        public async Task Start(CancellationToken token)
        {
            token.Register(Quit);

            Electron.App.WillQuit += (q) =>
            {
                Papercut.Service.Program.Shutdown();
                return Task.CompletedTask;
            };

            await Electron.WindowManager.CreateWindowAsync(
                new BrowserWindowOptions
                {
                    Width = 1152,
                    Height = 864,
                    Show = true,
                    BackgroundColor = "#f5f6f8",
                    Title = "Papercut",
                    Icon = WindowIcon()
                });

            var socket = GetElectronSocket();

            QuitOnConnectionProblems(socket, Socket.EVENT_CONNECT_ERROR);
            QuitOnConnectionProblems(socket, Socket.EVENT_RECONNECT_ERROR);
        }
    }
}