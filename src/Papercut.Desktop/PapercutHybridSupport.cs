using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.Extensions.PlatformAbstractions;
using Quobject.SocketIoClientDotNet.Client;

namespace Papercut.Desktop
{
    internal class PapercutHybridSupport
    {
        
        public static async void Bootstrap()
        {            
            Electron.App.WillQuit += Papercut.Service.Program.Shutdown;
            await Electron.WindowManager.CreateWindowAsync(new BrowserWindowOptions
            {
                Width = 1152,
                Height = 864,
                Show = true,
                BackgroundColor = "#f5f6f8",
                Title = "Papercut",
                Icon = WindowIcon()
            });
            
            var socketProp =  Electron.WindowManager
                .GetType().Assembly
                .GetType("ElectronNET.API.BridgeConnector")
                .GetProperty("Socket", BindingFlags.Static | BindingFlags.Public);
            var socket = socketProp.GetValue(null) as Socket;

            QuitOnConnectionProblems(socket, Socket.EVENT_CONNECT_ERROR);
            QuitOnConnectionProblems(socket, Socket.EVENT_RECONNECT_ERROR);
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
    }
}