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
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using ElectronNET.API;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.PlatformAbstractions;

using Papercut.Service.Infrastructure.WebServer;

using Serilog;
using Serilog.ExceptionalLogContext;

namespace Papercut.Service;

public class Program
{
    internal class BaseHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private static CancellationTokenSource _cancellationTokenSource;

    public static async Task Main(string[] args)
    {
        Console.Title = "Papercut.Service";

        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithExceptionalLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger(); // <-- 😎

        await RunAsync<BaseHostedService>(args);
    }

    public static async Task RunAsync<THostedService>(string[] args)
        where THostedService : class, IHostedService
    {
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            await CreateHostBuilder<THostedService>(args).Build().RunAsync(_cancellationTokenSource.Token);
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

    public static IHostBuilder CreateHostBuilder<THostedService>(string[] args)
        where THostedService : class, IHostedService =>
        Host.CreateDefaultBuilder(args)
            .UseContentRoot(PlatformServices.Default.Application.ApplicationBasePath)
            .UseSerilog(
                (context, services, configuration) => configuration
                    .Enrich.FromLogContext()
                    .Enrich.WithExceptionalLogContext()
                    .WriteTo.Console()
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services))
            .ConfigureServices(
                (context, sp) =>
                {
                    sp.AddSingleton<IConfiguration>(context.Configuration);
                    sp.AddSingleton<IHostEnvironment>(context.HostingEnvironment);
                    sp.AddHostedService<THostedService>();
                })
            .ConfigureWebHostDefaults(
                webBuilder =>
                {
                    webBuilder.UseElectron(args);
                    webBuilder.UseStartup<WebStartup>();
                });
}