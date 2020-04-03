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

namespace Papercut.Service.Services
{
    using System;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using Infrastructure.IPComm.Network;

    using Papercut.Common.Domain;
    using Papercut.Core.Domain.Application;
    using Papercut.Core.Domain.Network;
    using Papercut.Core.Domain.Network.Smtp;
    using Papercut.Core.Domain.Settings;
    using Papercut.Core.Infrastructure.Lifecycle;
    using Papercut.Core.Infrastructure.Server;
    using Papercut.Infrastructure.Smtp;
    using Papercut.Service.Helpers;

    using Serilog;

    public class PapercutServerService : IEventHandler<SmtpServerBindEvent>, IDisposable
    {
        private readonly PapercutSmtpServer _smtpServer;

        readonly IAppMeta _applicationMetaData;

        readonly ILogger _logger;

        readonly PapercutIPCommServer _ipCommServer;

        readonly IMessageBus _messageBus;

        readonly PapercutServiceSettings _serviceSettings;
        private readonly PapercutIPCommEndpoints _papercutIpCommEndpoints;

        public PapercutServerService(
            PapercutIPCommServer ipCommServer,
            PapercutSmtpServer smtpServer,
            PapercutServiceSettings serviceSettings,
            PapercutIPCommEndpoints papercutIpCommEndpoints,
            IAppMeta applicationMetaData,
            ILogger logger,
            IMessageBus messageBus)
        {
            _smtpServer = smtpServer;
            _serviceSettings = serviceSettings;
            _papercutIpCommEndpoints = papercutIpCommEndpoints;
            _applicationMetaData = applicationMetaData;
            _logger = logger;
            _messageBus = messageBus;
            _ipCommServer = ipCommServer;
        }

        public void Handle(SmtpServerBindEvent @event)
        {
            _logger.Information(
                "Received New Smtp Server Binding Settings from UI {@Event}",
                @event);

            // update settings...
            _serviceSettings.IP = @event.IP;
            _serviceSettings.Port = @event.Port;
            _serviceSettings.Save();

            // rebind the server...
            this.BindSMTPServer();
        }

        void BindSMTPServer()
        {
            _smtpServer.Stop();
            _smtpServer.Start(new EndpointDefinition(this._serviceSettings.IP, this._serviceSettings.Port));
        }

        public void Start()
        {
            this._messageBus.Publish(
                new PapercutServicePreStartEvent { AppMeta = _applicationMetaData });

            this._ipCommServer.ObserveStartServer(_papercutIpCommEndpoints.Service,
                TaskPoolScheduler.Default)
                .DelaySubscription(TimeSpan.FromSeconds(1)).Retry(5)
                .Subscribe(
                    (u) =>
                    {
                        /* next is not used */
                    },
                    (e) =>
                    _logger.Warning(
                        e,
                        "Unable to Create Papercut IPComm Server Listener on {IP}:{Port}. After 5 Retries. Failing",
                        _ipCommServer.ListenIpAddress,
                        _ipCommServer.ListenPort),
                    // on complete
                    () => { });

            _smtpServer.ObserveStartServer(_serviceSettings.IP, _serviceSettings.Port, TaskPoolScheduler.Default)
                .DelaySubscription(TimeSpan.FromSeconds(1)).Retry(5)
                .Subscribe(
                    (u) =>
                    {
                        /* next is not used */
                    },
                    (e) =>
                        _logger.Warning(
                            e, "Unable to Create SMTP Server Listener on {IP}:{Port}. After 5 Retries. Failing",
                            _serviceSettings.IP,
                            _serviceSettings.Port),
                    // on complete
                    () =>
                        this._messageBus.Publish(
                            new PapercutServiceReadyEvent {AppMeta = _applicationMetaData}));
        }

        public void Stop()
        {
            this._ipCommServer.Stop();

            _messageBus.Publish(new PapercutServiceExitEvent { AppMeta = _applicationMetaData });
        }

        public void Dispose()
        {
            this._ipCommServer?.Dispose();
        }
    }
}