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
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http.SelfHost;

    using Autofac;

    using Common.Domain;

    using Core.Domain.Settings;
    using Core.Infrastructure.Lifecycle;

    using Serilog;

    class WebServer : IEventHandler<PapercutServiceReadyEvent>, IEventHandler<PapercutClientReadyEvent>
    {
        readonly ILifetimeScope _scope;
        readonly ILogger _logger;
        readonly int _httpPort;
        const string BaseAddress = "http://127.0.0.1";
        const int DefaultHttpPort = 37408;

        private volatile bool _initialized = false;
        
        public WebServer(ILifetimeScope scope, ISettingStore settingStore, ILogger logger)
        {
            this._scope = scope;
            this._logger = logger.ForContext<WebServer>();
            this._httpPort = settingStore.GetOrSet("HttpPort", DefaultHttpPort, $"The Http Web UI Server listening port (Defaults to {DefaultHttpPort}).");
        }

        public async Task Handle(PapercutServiceReadyEvent @event)
        {
            this._logger.Debug("{@PapercutServiceReadyEvent}", @event);

            await StartHttpServer();
        }

        public async Task Handle(PapercutClientReadyEvent @event)
        {
            this._logger.Debug("{@PapercutClientReadyEvent}", @event);

            await StartHttpServer();
        }

        async Task StartHttpServer()
        {
            if (this._initialized) return;

            var uri = new UriBuilder(BaseAddress) { Port = this._httpPort }.Uri;

            try
            {
                this._initialized = true;

                var config = new HttpSelfHostConfiguration(uri);
                
                RouteConfig.Init(config, this._scope);

                await new HttpSelfHostServer(config).OpenAsync();

                this._logger.Information("[WebUI] Papercut Web UI is browsable at {@WebUiUri}", uri);
            }
            catch (Exception ex)
            {
                this._logger.Error(ex, "[WebUI] Can not start Web UI Http server at {@WebUiUri}", uri);
                this._initialized = false;
            }
        }
    }
}