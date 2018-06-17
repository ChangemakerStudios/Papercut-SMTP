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


namespace Papercut.Service.Infrastructure.WebServer
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Autofac;

    using Papercut.Common.Domain;
    using Papercut.Core.Domain.Settings;
    using Papercut.Core.Infrastructure.Lifecycle;
    using Papercut.Service.Web.Hosting;

    using Serilog;

    public class WebServer : IStartupService, IDisposable
    {
        readonly ILifetimeScope scope;
        readonly IMessageBus messageBus;

        private readonly ILogger _logger;

        readonly ISettingStore settingStore;

        ushort httpPort;

        const ushort DefaultHttpPort = 37408;

        public WebServer(ILifetimeScope scope, ISettingStore settingStore, IMessageBus messageBus, ILogger logger)
        {
            this.scope = scope;
            this.settingStore = settingStore;
            this.messageBus = messageBus;
            this._logger = logger;

            WebStartup.Scope = scope;

            this.httpPort = settingStore.Get("HttpPort", DefaultHttpPort);
        }

        public Task Start(CancellationToken token)
        {
            this._logger.Information("Starting Web Server on Port {httpPort}...", this.httpPort);
            
            HttpClient client;

            if (this.httpPort <= 0)
            {
                var server = WebStartup.StartInProcessServer(token);
                client = server.CreateClient();
            }
            else
            {
                WebStartup.Start(this.httpPort, token);
                client = new HttpClient() { BaseAddress = new Uri($"http://localhost:{this.httpPort}") };
            }

            this.messageBus.Publish(new PapercutWebServerReadyEvent { HttpClient = client });

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            WebStartup.Scope = null;
        }
    }
}