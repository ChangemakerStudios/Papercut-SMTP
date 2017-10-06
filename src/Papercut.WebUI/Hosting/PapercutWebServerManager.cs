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


namespace Papercut.WebUI.Hosting
{
    using System;
    using Microsoft.AspNetCore.Http;


    using Autofac;

    using Common.Domain;

    using Core.Domain.Settings;
    using Core.Infrastructure.Lifecycle;
    using System.Threading;

    public class PapercutWebServerManager : IEventHandler<PapercutServiceReadyEvent>, IDisposable
    {
        readonly ILifetimeScope scope;
        readonly Serilog.ILogger logger;
        readonly IMessageBus messageBus;

        readonly ushort httpPort;
        const string BaseAddress = "http://localhost:{0}";
        const ushort DefaultHttpPort = 37408;

        CancellationTokenSource serverCancellation;

        public PapercutWebServerManager(ILifetimeScope scope, 
        ISettingStore settingStore, 
        IMessageBus messageBus,
        Serilog.ILogger logger)
        {
            this.scope = scope;
            this.messageBus = messageBus;
            this.logger = logger;

            httpPort = settingStore.Get("HttpPort", DefaultHttpPort);
        }

        public void Handle(PapercutServiceReadyEvent @event)
        {
            if(httpPort <= 0)
            {
                return;
            }

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
    }
}