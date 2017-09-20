// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2017 Jaben Cargman
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


namespace Papercut.Module.WebUI
{
    using System;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;


    using Autofac;

    using Common.Domain;

    using Core.Domain.Settings;
    using Core.Infrastructure.Lifecycle;
    using System.Threading;
    using Microsoft.Extensions.PlatformAbstractions;
    using Autofac.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Serilog.Events;

    class WebServer : IEventHandler<PapercutServiceReadyEvent>, IDisposable
    {
        readonly ILifetimeScope scope;
        readonly Serilog.ILogger logger;

        readonly ushort httpPort;
        const string BaseAddress = "http://localhost:{0}";
        const ushort DefaultHttpPort = 37408;

        CancellationTokenSource serverCancellation;

        public WebServer(ILifetimeScope scope, ISettingStore settingStore, Serilog.ILogger logger)
        {
            this.scope = scope;
            this.logger = logger;
            httpPort = settingStore.Get("HttpPort", DefaultHttpPort);
        }

        public void Handle(PapercutServiceReadyEvent @event)
        {
            StartHttpServer();
        }
        

        void StartHttpServer()
        {
            serverCancellation = new CancellationTokenSource();
            WebStartup.Scope = scope;
            WebStartup.Start(httpPort, serverCancellation.Token);
        }

        void IDisposable.Dispose()
        {
            if (serverCancellation != null) {
                serverCancellation.Cancel();
                WebStartup.Scope = null;

                serverCancellation.Dispose();
                serverCancellation = null;
            }            
        }

        class WebStartup {
            public static ILifetimeScope Scope { get; set; }
            public static void Start(ushort httpPort, CancellationToken cancellation)
            {
                var hostBuilder = new WebHostBuilder();
                hostBuilder
                    .UseWebRoot(PlatformServices.Default.Application.ApplicationBasePath)
                    .UseKestrel()
                    .UseStartup<WebStartup>()
                    .UseUrls($"http://*:{httpPort}");

                var host = hostBuilder.Build();
                host.Run(cancellation);
            }

            public IServiceProvider ConfigureServices(IServiceCollection services)
            {
                services.AddLogging();
                services.AddMvcCore();
                services.AddMemoryCache();
                

                var builder = new ContainerBuilder();
                builder.Populate(services);

                #pragma warning disable CS0618 // Type or member is obsolete
                builder.Update(Scope.ComponentRegistry);
                return new AutofacServiceProvider(Scope);
            }

            public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
            {
                loggerFactory.AddProvider(new SerilogLoggerProvider());
            }


            class SerilogLoggerProvider : ILoggerProvider
            {
                public ILogger CreateLogger(string categoryName)
                {
                    return new SerilogLoggerAdapter(Scope.Resolve<Serilog.ILogger>());
                }

                public void Dispose()
                {
                    
                }
            }

            class SerilogLoggerAdapter : ILogger, IDisposable
            {
                private Serilog.ILogger logger;

                public SerilogLoggerAdapter(Serilog.ILogger logger)
                {
                    this.logger = logger;
                }

                public IDisposable BeginScope<TState>(TState state)
                {
                    return this;
                }

                public bool IsEnabled(LogLevel logLevel)
                {
                    return true;
                }

                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
                {
                    // var serilogLevel = levelMapping[logLevel];
                    logger.Write(LogEventLevel.Debug, exception, "");
                }

                // static Dictionary<LogLevel, LogEventLevel> levelMapping;

                void IDisposable.Dispose()
                {

                }
            }
        }
        
    }
}