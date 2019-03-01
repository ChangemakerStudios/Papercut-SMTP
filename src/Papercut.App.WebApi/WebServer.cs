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


namespace Papercut.App.WebApi
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web.Http.SelfHost;

    using Autofac;

    using Papercut.Common.Domain;
    using Papercut.Core.Domain.Settings;
    using Papercut.Core.Infrastructure.Lifecycle;

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
            this._httpPort = settingStore.GetOrSet("HttpPort", DefaultHttpPort, $"The Http Web UI Server listening port (Defaults to {DefaultHttpPort}). Set to 0 to disable Http Web UI Server.");
        }

        public void Handle(PapercutServiceReadyEvent @event)
        {
            this._logger.Debug("{@PapercutServiceReadyEvent}", @event);

            if (this._httpPort != 0)
            {
                this.StartHttpServer();
            }
        }

        public void Handle(PapercutClientReadyEvent @event)
        {
            this._logger.Debug("{@PapercutClientReadyEvent}", @event);

            if (this._httpPort != 0)
            {
                this.StartHttpServer();
            }
        }

        void StartHttpServer()
        {
            if (this._initialized) return;

            var uri = new UriBuilder(BaseAddress) { Port = this._httpPort }.Uri;

            try
            {
                var config = new HttpSelfHostConfiguration(uri);

                RouteConfig.Init(config, this._scope);

                new HttpSelfHostServer(config).OpenAsync().Wait();

                this._initialized = true;

                this._logger.Information("[WebUI] Papercut Web UI is browsable at {@WebUiUri}", uri);
            }
            catch (HttpListenerException ex)
            {
                this._logger.Warning(ex, "[WebUI] Run with elevated permissions (Administrator)");
                this._initialized = false;
            }
            catch (Exception ex)
            {
                this._logger.Error(ex, "[WebUI] Can not start Web UI Http server at {@WebUiUri}", uri);
                this._initialized = false;
            }
        }
    }
}