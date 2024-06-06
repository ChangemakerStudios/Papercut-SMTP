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


using Autofac;

using Papercut.Core.Infrastructure.Container;
using Papercut.Core.Infrastructure.Logging;

using Serilog.Extensions.Logging;

using Velopack;

namespace Papercut;

public class Program
{
    internal static IContainer Container { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        Log.Logger = BootstrapLogger.CreateBootstrapLogger(args);

        try
        {
            Log.Information("Running Velopack...");

            var microsoftLogger = new SerilogLoggerFactory().CreateLogger(nameof(VelopackApp));

            // It's important to Run() the VelopackApp as early as possible in app startup.
            VelopackApp.Build()
                .WithFirstRun(
                    (v) =>
                    {
                        /* Your first run code here */
                    })
                .Run(microsoftLogger);

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