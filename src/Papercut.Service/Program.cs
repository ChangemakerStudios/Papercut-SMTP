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


namespace Papercut.Service;

using System.Reflection;

using Autofac.Extensions.DependencyInjection;

using Common.Helper;

using Core.Infrastructure.Logging;

using ElectronNET.API;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class Program
{
    private static readonly IAppMeta AppMeta = new ApplicationMeta("Papercut.SMTP.Service", Assembly.GetExecutingAssembly().GetVersion());

    private static readonly CancellationTokenSource _cancellationTokenSource = new();

    public static async Task Main(string[] args)
    {
        Console.Title = AppMeta.AppName;

        Log.Logger = BootstrapLogger.CreateBootstrapLogger(args);

        await RunAsync(args);
    }

    public static async Task RunAsync(string[] args)
    {
        try
        {
            await CreateWebApp(args).RunAsync(_cancellationTokenSource.Token);
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

    private static WebApplication CreateWebApp(string[] args)
    {
        var applicationOptions = new WebApplicationOptions
            { ContentRootPath = AppContext.BaseDirectory, Args = args };

        var builder = WebApplication.CreateBuilder(applicationOptions);

        builder.Host.UseWindowsService(
            options =>
            {
                options.ServiceName = AppMeta.AppName;
            }).ConfigureServices(
            (context, sp) =>
            {
                sp.AddSingleton(context.Configuration);
                sp.AddSingleton(context.HostingEnvironment);

                if (HybridSupport.IsElectronActive)
                    sp.AddHostedService<ElectronService>();
            });

        builder.Logging.ClearProviders();
        builder.Host.UseSerilog();
        builder.WebHost.UseElectron(args);

        var startup = new PapercutServiceStartup();

        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory(startup.ConfigureContainer));
        startup.ConfigureServices(builder.Services, builder.Configuration);

        var webApp = builder.Build();

        startup.Configure(webApp);

        return webApp;
    }

    #region Begin Static Container Registrations

    static void Register(ContainerBuilder builder)
    {
        builder.RegisterInstance(AppMeta).As<IAppMeta>().SingleInstance();
    }

    #endregion
}