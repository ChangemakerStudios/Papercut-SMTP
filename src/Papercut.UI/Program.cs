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

using Papercut.Core;
using Papercut.Core.Domain.Application;
using Papercut.Core.Infrastructure.Container;
using Papercut.Core.Infrastructure.Logging;
using Papercut.Infrastructure;

using Velopack;

namespace Papercut;

public class Program
{
    [DllImport("shell32.dll", ExactSpelling = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelId(
        [MarshalAs(UnmanagedType.LPWStr)] string appId);

    internal static readonly IAppMeta AppMeta = new ApplicationMeta(AppConstants.ApplicationName, Assembly.GetExecutingAssembly().GetVersion());

    internal static IContainer Container { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        Log.Logger = BootstrapLogger.CreateBootstrapLogger(AppMeta, args);

        try
        {
            Log.Information("Running Velopack...");

            // It's important to Run() the VelopackApp as early as possible in app startup.
            VelopackApp.Build()
                .SetLogger(new VelopackBridgeLogger(Log.Logger))
                .Run();

            // Velopack sets AppUserModelId to "velopack.PapercutSMTP" which appears
            // as the notification source name on Windows 10+. Override it so notifications
            // display "Papercut SMTP" instead. Can be replaced with VelopackApp.SetAppUserModelId()
            // once a Velopack NuGet release includes PR #777.
            try
            {
                const string appUserModelId = "ChangemakerStudios.PapercutSMTP";
                int hr = SetCurrentProcessExplicitAppUserModelId(appUserModelId);
                if (hr != 0)
                    Log.Warning("Failed to set AppUserModelId, HRESULT: 0x{HResult:X8}", hr);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to set AppUserModelId");
            }

            Log.Information("Launching Papercut SMTP App...");

            using (Container = new SimpleContainer<PapercutUIModule>().Build())
            {
                var app = new App();
                app.InitializeComponent();
                app.Run();
            }

            Log.Information("Shutting down...");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unhandled Exception");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}