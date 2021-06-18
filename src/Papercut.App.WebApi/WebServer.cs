// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2021 Jaben Cargman
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
    using System.Web.Http;

    using Autofac;
    using Autofac.Util;

    using Microsoft.Owin.Hosting;

    using Owin;

    using Papercut.Core.Domain.Settings;
    using Papercut.Core.Domain.WebServer;

    using Serilog;

    internal class PapercutWebServer : Disposable, IPapercutWebServer
    {
        const string DefaultHttpBaseAddress = "http://127.0.0.1";

        const int DefaultHttpPort = 37408;

        readonly string _httpBaseAddress;

        readonly int _httpPort;

        readonly bool _httpServerEnabled;

        readonly ILogger _logger;

        readonly ILifetimeScope _scope;

        volatile bool _isActive;

        IDisposable _webAppDisposable;

        public PapercutWebServer(ILifetimeScope scope, ISettingStore settingStore, ILogger logger)
        {
            this._scope = scope;
            this._logger = logger.ForContext<PapercutWebServer>();
            this._httpServerEnabled = settingStore.GetOrSet("HttpServerEnabled", true, $"Is the Papercut Web UI Server enabled (Defaults to true)?");
            this._httpBaseAddress = settingStore.GetOrSet("HttpBaseAddress", DefaultHttpBaseAddress, $"The Papercut Web UI Server listening address (Defaults to {DefaultHttpBaseAddress}).");
            this._httpPort = settingStore.GetOrSet("HttpPort", DefaultHttpPort, $"The Papercut Web UI Server listening port (Defaults to {DefaultHttpPort}).");
        }

        public Task StartAsync()
        {
            if (this._httpServerEnabled)
            {
                Task.Factory.StartNew(this.StartHttpServer);
            }
            else
            {
                this._logger.Information("Web Server is disabled");
            }

            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            this._webAppDisposable?.Dispose();
            this._isActive = false;

            return Task.CompletedTask;
        }

        public bool IsActive => this._isActive;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._webAppDisposable?.Dispose();
            }
        }

        void StartHttpServer()
        {
            if (this._isActive) return;

            var uri = new UriBuilder(this._httpBaseAddress) { Port = this._httpPort }.Uri;

            try
            {
                this._webAppDisposable = WebApp.Start(uri.ToString(), builder =>
                {
                    var config = new HttpConfiguration();

                    RouteConfig.Init(config, this._scope);

                    builder.UseWebApi(config);
                });

                this._isActive = true;

                this._logger.Information("[WebUI] Papercut Web UI is browsable at {@WebUiUri}", uri);
            }
            catch (HttpListenerException ex)
            {
                this._logger.Warning(ex, "[WebUI] Run with elevated permissions (Administrator)");
                this._isActive = false;
            }
            catch (Exception ex)
            {
                this._logger.Error(ex, "[WebUI] Can not start Web UI Http server at {@WebUiUri}", uri);
                this._isActive = false;
            }
        }
    }
}