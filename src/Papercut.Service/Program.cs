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

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Serilog.Debugging;
using Serilog.ExceptionalLogContext;

namespace Papercut.Service;

public class Program
{
    private static readonly CancellationTokenSource _cancellationTokenSource = new();

    public static async Task Main(string[] args)
    {
        Console.Title = "Papercut.Service";

        TaskScheduler.UnobservedTaskException += (sender, e) => Log.Error(e.Exception, "Unobserved Task Exception");
        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) => Log.Error(eventArgs.ExceptionObject as Exception, "Unhandled Exception");

        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithExceptionalLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger(); // <-- 😎

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

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseContentRoot(AppContext.BaseDirectory)
            .UseSerilog(CreateDefaultLogger)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureServices(
                (context, sp) =>
                {
                    sp.AddSingleton<IConfiguration>(context.Configuration);
                    sp.AddSingleton<IHostEnvironment>(context.HostingEnvironment);
                })
            .ConfigureWebHostDefaults(
                webBuilder =>
                {
                    webBuilder.UseElectron(args);
                    webBuilder.UseStartup<PapercutServiceStartup>();
                })
            .ConfigureServices(
                (context, sp) =>
                {
                    if (HybridSupport.IsElectronActive)
                        sp.AddHostedService<ElectronService>();
                });

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