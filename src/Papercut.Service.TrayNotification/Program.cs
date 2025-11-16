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


using System.Reflection;

using Autofac;

using Papercut.Common.Helper;
using Papercut.Core.Domain.Application;
using Papercut.Core.Domain.Paths;
using Papercut.Core.Infrastructure.Consoles;
using Papercut.Core.Infrastructure.Container;
using Papercut.Core.Infrastructure.Logging;
using Papercut.Service.TrayNotification.AppLayer;
using Papercut.Service.TrayNotification.Infrastructure;

namespace Papercut.Service.TrayNotification;

internal static class Program
{
    private static readonly IAppMeta AppMeta = new ApplicationMeta("Papercut.Service.TrayNotification", Assembly.GetExecutingAssembly().GetVersion());

    internal static IContainer Container { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        Log.Logger = BootstrapLogger.CreateBootstrapLogger(AppMeta, args);

        if (ConsoleHelpers.HasConsole())
        {
            Console.Title = AppMeta.AppName;
        }

        try
        {
            ApplicationConfiguration.Initialize();

            using (Container = new SimpleContainer<PapercutServiceTrayModule>().Build())
            {
                Log.Logger.Information("Logging to Path {Path}", Container.Resolve<LoggingPathConfigurator>().DefaultSavePath);

                var coordinator = Container.Resolve<ServiceTrayCoordinator>();
                Application.Run();
            }
        }
        catch (Exception ex) when (ex is not TaskCanceledException and not ObjectDisposedException)
        {
            Log.Fatal(ex, "Fatal error in Service Tray Manager");
            MessageBox.Show(
                $"Fatal error: {ex.Message}",
                "Papercut Service Tray Manager Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    #region Begin Static Container Registrations

    private static void Register(ContainerBuilder builder)
    {
        builder.RegisterInstance(AppMeta).As<IAppMeta>().SingleInstance();
    }

    #endregion
}
