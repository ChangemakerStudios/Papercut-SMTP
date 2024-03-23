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


using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

using Papercut.Rules;
using Papercut.Service.Infrastructure.Servers;

namespace Papercut.Service;

internal class PapercutServiceStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
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

        services.AddOptions<SmtpServerOptions>("SmtpServer");

        services.AddSingleton(s => s.GetRequiredService<IOptions<SmtpServerOptions>>().Value);

        // hosted services
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
        app.UseRouting();

        app.UseSerilogRequestLogging();

        app.UseEndpoints(
            s =>
            {
                s.MapControllers();
            });
    }
}