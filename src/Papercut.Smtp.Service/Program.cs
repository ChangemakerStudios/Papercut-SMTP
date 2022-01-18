// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2022 Jaben Cargman
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

namespace Papercut.Smtp.Service;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.Title = AppConstants.ApplicationName;
        BootstrapLogger.SetRootGlobal();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            var startup = new Startup(builder.Configuration);

            Log.CloseAndFlush();

            Log.Logger = new LoggerConfiguration().MinimumLevel.Verbose()
                .Enrich.With<EnvironmentEnricher>()
                .Enrich.WithProperty("AppName", AppConstants.ApplicationName)
                .Enrich.WithProperty("AppVersion", FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion)
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();

            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory(startup.ConfigureContainer));
            builder.WebHost.ConfigureServices(startup.ConfigureServices).UseSerilog();

            var app = builder.Build();

            app.Urls.Add("http://localhost:8000");
            
            startup.Configure(app, app.Environment);

            await app.RunAsync();
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

    //public static IHostBuilder CreateHostBuilder(string[] args)
    //{
    //    return WebApplication.CreateBuilder(args)
    //        .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    //        .ConfigureWebHostDefaults(
    //            webBuilder =>
    //            {
    //                webBuilder.UseStartup<Startup>()
    //                    .UseSerilog(
    //                        (b, c) =>
    //                        {
    //                            c.MinimumLevel.Information()
    //                                .Enrich.With<EnvironmentEnricher>()
    //                                .WriteTo.Console()
    //                                .Enrich.WithProperty(
    //                                    "AppName",
    //                                    AppConstants.ApplicationName)
    //                                .Enrich.WithProperty(
    //                                    "AppVersion",
    //                                    FileVersionInfo.GetVersionInfo(
    //                                            Assembly.GetExecutingAssembly().Location)
    //                                        .ProductVersion)
    //                                .ReadFrom.Configuration(b.Configuration);
    //                        });
    //            });
    //}
    ////.UseWindowsService();
}