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


namespace Papercut.Service.Web.Hosting
{
    using System;
    using Microsoft.AspNetCore.Http;


    using Autofac;
    using Common.Domain;
    using Core.Domain.Settings;
    using System.Threading;
    using System.Net.Http;

    public class WebServer : IDisposable
    {
        readonly ILifetimeScope scope;
        readonly Serilog.ILogger logger;
        readonly IMessageBus messageBus;
        readonly ISettingStore settingStore;

        ushort httpPort;
        const ushort DefaultHttpPort = 37408;

        CancellationTokenSource serverCancellation;

        public WebServer(ILifetimeScope scope, ISettingStore settingStore, IMessageBus messageBus, Serilog.ILogger logger)
        {
            this.scope = scope;
            this.settingStore = settingStore;
            this.messageBus = messageBus;
            this.logger = logger;
        }

        public void Start()
        {
            httpPort = settingStore.Get("HttpPort", DefaultHttpPort);

            serverCancellation = new CancellationTokenSource();
            WebStartup.Scope = scope;
            
            HttpClient client;
            if (httpPort <= 0)
            {
                var server = WebStartup.StartInProcessServer(serverCancellation.Token);
                client = server.CreateClient();
            }else{
                WebStartup.Start(httpPort, serverCancellation.Token);
                client = new HttpClient(){ BaseAddress = new Uri($"http://localhost:{httpPort}") };
            }

            messageBus.Publish(new PapercutWebServerReadyEvent{ HttpClient = client });
        }

        public void Stop()
        {
            if (serverCancellation == null)
            {
                return;
            }

            serverCancellation.Cancel();
            serverCancellation.Dispose();
            serverCancellation = null;
            WebStartup.Scope = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}