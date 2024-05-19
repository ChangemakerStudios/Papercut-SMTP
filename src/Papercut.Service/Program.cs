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


using Autofac.Extensions.DependencyInjection;

using ElectronNET.API;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Papercut.Core.Infrastructure.Logging;

using Serilog.Core;
using Serilog.Debugging;
using Serilog.ExceptionalLogContext;

namespace Papercut.Service;

public class Program
{
    const string AppName= "Papercut.SMTP.Service";

    private static readonly CancellationTokenSource _cancellationTokenSource = new();

    public static async Task Main(string[] args)
    {
        Console.Title = AppName;

        Log.Logger = BootstrapLogger.CreateBootstrapLogger(args);

        await RunAsync(args);
    }

    public static async Task RunAsync(string[] args)
    {
        try
        {
            await CreateHostBuilder(args).Build().RunAsync(_cancellationTokenSource.Token);
        }
        catch (Exception ex) when (ex is not TaskCanceledException and not ObjectDisposedException)
        {
            Log.Fatal(ex, "An unhandled exception occured during bootstrapping");
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
            await Log.CloseAndFlushAsync();
        }
    }

    public static void Shutdown()
    {
        _cancellationTokenSource.Cancel();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .UseWindowsService(
                options =>
                {
                    options.ServiceName = AppName;
                })
            .UseContentRoot(AppContext.BaseDirectory)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureServices(
                (context, sp) =>
                {
                    sp.AddSingleton(context.Configuration);
                    sp.AddSingleton(context.HostingEnvironment);
                    sp.AddSingleton(new LoggingLevelSwitch());
                })
            .ConfigureWebHostDefaults(
                (webBuilder) =>
                {
                    webBuilder.ConfigureLogging(
                        s =>
                        {
                            s.ClearProviders();
                        });
                    webBuilder.UseElectron(args);
                    webBuilder.UseStartup<PapercutServiceStartup>();
                })
            .UseSerilog(CreateDefaultLogger)
            .ConfigureServices(
                (context, sp) =>
                {
                    if (HybridSupport.IsElectronActive)
                        sp.AddHostedService<ElectronService>();
                });
    }

    private static void CreateDefaultLogger(HostBuilderContext context, IServiceProvider services, LoggerConfiguration configuration)
    {
        var appMeta = services.GetRequiredService<IAppMeta>();

        configuration
            .Enrich.FromLogContext()
            .Enrich.WithExceptionalLogContext()
            .Enrich.WithProperty("AppName", appMeta.AppName)
            .Enrich.WithProperty("AppVersion", appMeta.AppVersion)
            .WriteTo.Console()
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services);

        SelfLog.Enable(s => Console.Error.WriteLine(s));
    }
}