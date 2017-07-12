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
    using System.ServiceModel;
    using System.Web.Http.SelfHost;

    using Autofac;

    using Common.Domain;

    using Core.Domain.Settings;
    using Core.Infrastructure.Lifecycle;

    using Serilog;

    class WebServer : IEventHandler<PapercutServiceReadyEvent>, IEventHandler<PapercutClientReadyEvent>
    {
        readonly ILifetimeScope scope;
        readonly ILogger logger;

        readonly int httpPort;
        const string BaseAddress = "http://localhost:{0}";
        const int DefaultHttpPort = 37408;

        public WebServer(ILifetimeScope scope, ISettingStore settingStore, ILogger logger)
        {
            this.scope = scope;
            this.logger = logger;
            httpPort = settingStore.Get("HttpPort", DefaultHttpPort);
        }

        public void Handle(PapercutServiceReadyEvent @event)
        {
            StartHttpServer();
        }

        public void Handle(PapercutClientReadyEvent @event)
        {
            StartHttpServer();
        }

        void StartHttpServer()
        {
            try
            {
                var config = new HttpSelfHostConfiguration(string.Format(BaseAddress, httpPort))
                {
                    HostNameComparisonMode = HostNameComparisonMode.WeakWildcard
                };
                RouteConfig.Init(config, scope);
                new HttpSelfHostServer(config).OpenAsync().Wait();

                logger.Information($"WebUI server started at port {httpPort}.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Can not start HTTP server at port {httpPort}.");
            }
        }
    }
}