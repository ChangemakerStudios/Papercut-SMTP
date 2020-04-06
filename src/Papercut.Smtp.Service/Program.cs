namespace Papercut.Smtp.Service
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Threading.Tasks;

    using Autofac.Extensions.DependencyInjection;

    using Core;
    using Core.Infrastructure;
    using Core.Infrastructure.Logging;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;

    using Serilog;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.Title = AppConstants.ApplicationName;
            BootstrapLogger.SetRootGlobal();

            try
            {
                var build = CreateHostBuilder(args).Build();

                Log.CloseAndFlush();
                Log.Logger = build.Services.Resolve<ILogger>();

                await build.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                        .UseSerilog((b, c) =>
                        {
                            c.MinimumLevel.Information()
                                .Enrich.With<EnvironmentEnricher>()
                                .WriteTo.Console()
                                .Enrich.WithProperty("AppName", AppConstants.ApplicationName)
                                .Enrich.WithProperty("AppVersion",
                                    FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location)
                                        .ProductVersion)
                                .ReadFrom.Configuration(b.Configuration);
                        });
                })
                .UseWindowsService();
    }
}
