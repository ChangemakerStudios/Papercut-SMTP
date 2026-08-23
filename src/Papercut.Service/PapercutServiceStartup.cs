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


namespace Papercut.Service;

using Application.Messages;

using Infrastructure.MessageWatching;
using Infrastructure.Servers;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using Application.Mcp;

using Rules;

using Papercut.Service.Domain;
using Papercut.Service.Infrastructure.Configuration;

internal class PapercutServiceStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
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
                        c.AllowAnyMethod();
                    });
            });

        // Add SignalR
        services.AddSignalR();

        services.Configure<SmtpServerOptions>(configuration.GetSection("SmtpServer"));

        services
            .AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssembly();

        // hosted services
        services.AddHostedService<PapercutServerHostedService>();
        services.AddHostedService<MessageWatcherHostedService>();
    }

    IEnumerable<Module> GetModules()
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
        foreach (var module in GetModules())
        {
            builder.RegisterModule(module);
        }
    }

    public void Configure(WebApplication app)
    {
        var pathPrefix = GetHttpPathPrefix(app);

        if (!string.IsNullOrEmpty(pathPrefix))
        {
            Log.Information("Serving HTTP under path prefix {HttpPathPrefix}", pathPrefix);

            app.UsePathBase(pathPrefix);

            // redirect the bare prefix ("/webmail") to "/webmail/" so the web UI's
            // relative asset and api urls resolve against the prefix
            app.Use(
                async (context, next) =>
                {
                    if (context.Request.PathBase.HasValue && !context.Request.Path.HasValue)
                    {
                        context.Response.Redirect(context.Request.PathBase + "/");
                        return;
                    }

                    await next();
                });
        }

        app.UseRouting();

        app.UseCors();

        // Request traffic is background noise for a mail viewer: a single click
        // fetches the ref, the detail and the rendered body, so logging each one
        // at INF buried the events people actually open the log for (messages
        // received, rules run, failures). Successful requests go to DBG, where
        // the Log view's level picker can still bring them back.
        app.UseSerilogRequestLogging(
            options => options.GetLevel = (httpContext, _, ex) =>
            {
                if (ex != null || httpContext.Response.StatusCode >= 500)
                    return Serilog.Events.LogEventLevel.Error;

                if (httpContext.Response.StatusCode >= 400)
                    return Serilog.Events.LogEventLevel.Warning;

                // the web Log view polls this endpoint; even at DBG its own
                // tailing would crowd out everything else
                if (httpContext.Request.Path.StartsWithSegments("/api/logs"))
                    return Serilog.Events.LogEventLevel.Verbose;

                return Serilog.Events.LogEventLevel.Debug;
            });

        var mcpEnabled = McpServerSettings.IsEnabled(
            app.Services.GetRequiredService<ISettingStore>(),
            app.Configuration);

        if (mcpEnabled)
        {
            Log.Information(
                "MCP server is enabled -- serving MCP endpoint at {McpEndpointPath}",
                $"{pathPrefix}{McpServerSettings.EndpointPath}");
        }
        else
        {
            Log.Information(
                "MCP server is disabled (set {McpEnabledSettingKey} to true to enable)",
                McpServerSettings.EnabledSettingKey);
        }

        app.UseEndpoints(
            s =>
            {
                s.MapControllers();
                s.MapHub<MessagesHub>("/hubs/messages");

                if (mcpEnabled)
                {
                    s.MapMcp(McpServerSettings.EndpointPath);
                }
            });
    }

    private static string GetHttpPathPrefix(WebApplication app)
    {
        var settingStore = app.Services.GetRequiredService<ISettingStore>();
        var pathPrefix = settingStore.Get("HttpPathPrefix", app.Configuration["HttpPathPrefix"]);

        if (string.IsNullOrWhiteSpace(pathPrefix)) return string.Empty;

        pathPrefix = "/" + pathPrefix.Trim().Trim('/');

        return pathPrefix == "/" ? string.Empty : pathPrefix;
    }
}