// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2026 Jaben Cargman
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


using Avalonia;

using Papercut.Desktop.Platform;

using Serilog.Events;

using Velopack;

namespace Papercut.Desktop;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        // must run first: Velopack's install/update/uninstall hooks run through here
        VelopackApp.Build()
            .OnBeforeUninstallFastCallback(_ =>
            {
                try
                {
                    PlatformServices.CreateRunAtStartup().Set(false);
                }
                catch
                {
                    // never block an uninstall
                }
            })
            .Run();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(Environment.GetEnvironmentVariable("PAPERCUT_DEBUG") == "1" ? LogEventLevel.Debug : LogEventLevel.Information)
            .WriteTo.File(
                Path.Combine(ShellPaths.LogDirectory, "papercut-desktop-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();

        try
        {
            using var instance = SingleInstance.TryAcquire();

            if (instance == null)
            {
                Log.Information("Papercut is already running -- asked it to show its window");
                return 0;
            }

            var settings = ShellSettings.Load();
            V7Migration.Run(settings);

            App.StartupSettings = settings;
            App.Instance = instance;

            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnExplicitShutdown);

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Papercut desktop crashed");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
