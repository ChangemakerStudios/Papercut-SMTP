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


using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using Papercut.Rules;
using Papercut.Service.Infrastructure.Configuration;
using Papercut.Service.Infrastructure.Middleware;
using Papercut.Service.Infrastructure.Servers;

namespace Papercut.Service;

internal class PapercutServiceStartup
{
    private IConfiguration? _configuration;

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        _configuration = configuration;

        services.AddLogging();
        services.AddMemoryCache();

        services.AddMvc().AddControllersAsServices();

        services.AddHttpContextAccessor();

        services.AddCors(
            s =>
            {
                s.AddDefaultPolicy(
                    c =>
                    {
                        c.AllowAnyHeader();
                        c.AllowAnyOrigin();
                    });
            });

        services.Configure<SmtpServerOptions>(configuration.GetSection("SmtpServer"));
        services.AddSingleton(s => s.GetRequiredService<IOptions<SmtpServerOptions>>().Value);

        // hosted services
        services.AddHostedService<SmtpServerOptionsInitializer>();
        services.AddHostedService<PapercutServerHostedService>();
    }

    IEnumerable<Autofac.Module> GetModules()
    {
        yield return new PapercutCoreModule();
        yield return new PapercutMessageModule();
        yield return new PapercutRuleModule();
        yield return new PapercutIPCommModule();
        yield return new PapercutRuleModule();
        yield return new PapercutSmtpModule();

        yield return new PapercutServiceModule();
    }

    [UsedImplicitly]
    public void ConfigureContainer(ContainerBuilder builder)
    {
        foreach (var module in this.GetModules())
        {
            builder.RegisterModule(module);
        }
    }

    public void Configure(IApplicationBuilder app)
    {
        // Get IP allowlist configuration for web UI filtering
        // Use SmtpServer:AllowedHosts to avoid conflict with ASP.NET Core's root AllowedHosts (HostFiltering)
        // Can be set via environment variable: SmtpServer__AllowedHosts=192.168.1.0/24,10.0.0.0/8
        var allowedHosts = _configuration?.GetValue<string>("SmtpServer:AllowedHosts") ?? "*";
        var logger = Log.ForContext<PapercutServiceStartup>();

        // Apply IP allowlist filtering before routing
        app.UseIpAllowlist(allowedHosts, logger);

        app.UseRouting();

        app.UseSerilogRequestLogging();

        app.UseEndpoints(
            s =>
            {
                s.MapControllers();
            });
    }
}