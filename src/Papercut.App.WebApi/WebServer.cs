// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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
    using System.Web.Http;

    using Autofac;
    using Autofac.Util;

    using Common.Domain;

    using Core.Domain.Settings;
    using Core.Infrastructure.Lifecycle;

    using Microsoft.Owin.Hosting;

    using Owin;

    using Serilog;

    internal class WebServer : Disposable, IEventHandler<PapercutServiceReadyEvent>, IEventHandler<PapercutClientReadyEvent>
    {
        const string DefaultHttpBaseAddress = "http://127.0.0.1";
        const int DefaultHttpPort = 37408;
        readonly string _httpBaseAddress;
        readonly int _httpPort;
        readonly bool _httpServerEnabled;
        readonly ILogger _logger;
        readonly ILifetimeScope _scope;

        volatile bool _initialized;
        IDisposable _webAppDisposable;

        public WebServer(ILifetimeScope scope, ISettingStore settingStore, ILogger logger)
        {
            _scope = scope;
            _logger = logger.ForContext<WebServer>();
            _httpServerEnabled = settingStore.GetOrSet("HttpServerEnabled", true, $"Is the Papercut Web UI Server enabled (Defaults to true)?");
            _httpBaseAddress = settingStore.GetOrSet("HttpBaseAddress", DefaultHttpBaseAddress, $"The Papercut Web UI Server listening address (Defaults to {DefaultHttpBaseAddress}).");
            _httpPort = settingStore.GetOrSet("HttpPort", DefaultHttpPort, $"The Papercut Web UI Server listening port (Defaults to {DefaultHttpPort}).");
        }

        public void Handle(PapercutClientReadyEvent @event)
        {
            _logger.Debug("{@PapercutClientReadyEvent}", @event);

            if (_httpServerEnabled)
            {
                StartHttpServer();
            }
        }

        public void Handle(PapercutServiceReadyEvent @event)
        {
            _logger.Debug("{@PapercutServiceReadyEvent}", @event);

            if (_httpServerEnabled)
            {
                StartHttpServer();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _webAppDisposable?.Dispose();
            }
        }

        void StartHttpServer()
        {
            if (_initialized) return;

            var uri = new UriBuilder(_httpBaseAddress) { Port = _httpPort }.Uri;

            try
            {
                _webAppDisposable = WebApp.Start(uri.ToString(), builder =>
                {
                    var config = new HttpConfiguration();

                    RouteConfig.Init(config, _scope);

                    builder.UseWebApi(config);
                });

                _initialized = true;

                _logger.Information("[WebUI] Papercut Web UI is browsable at {@WebUiUri}", uri);
            }
            catch (HttpListenerException ex)
            {
                _logger.Warning(ex, "[WebUI] Run with elevated permissions (Administrator)");
                _initialized = false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[WebUI] Can not start Web UI Http server at {@WebUiUri}", uri);
                _initialized = false;
            }
        }
    }
}