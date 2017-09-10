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

namespace Papercut.Service.Services
{
    using System;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;

    using Papercut.Common.Domain;
    using Papercut.Core.Domain.Application;
    using Papercut.Core.Domain.Network;
    using Papercut.Core.Domain.Network.Smtp;
    using Papercut.Core.Domain.Settings;
    using Papercut.Core.Infrastructure.Lifecycle;
    using Papercut.Network;
    using Papercut.Network.Protocols;
    using Papercut.Network.Smtp;
    using Papercut.Service.Helpers;

    using Serilog;

    public class PapercutServerService : IEventHandler<SmtpServerBindEvent>, IDisposable
    {
        readonly IAppMeta _applicationMetaData;

        readonly ILogger _logger;

        readonly IServer _papercutServer;

        readonly IMessageBus _messageBus;

        readonly PapercutServiceSettings _serviceSettings;

        readonly IServer _smtpServer;

        public PapercutServerService(
            Func<ServerProtocolType, IServer> serverFactory,
            PapercutServiceSettings serviceSettings,
            IAppMeta applicationMetaData,
            ILogger logger,
            IMessageBus messageBus)
        {
            _serviceSettings = serviceSettings;
            _applicationMetaData = applicationMetaData;
            _logger = logger;
            _messageBus = messageBus;
            _smtpServer = serverFactory(ServerProtocolType.Smtp);
            _papercutServer = serverFactory(ServerProtocolType.Papercut);
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
            BindSMTPServer();
        }

        void BindSMTPServer()
        {
            _smtpServer.Stop();
            _smtpServer.Listen(_serviceSettings.IP, _serviceSettings.Port);
        }

        public void Start()
        {
            this._messageBus.Publish(
                new PapercutServicePreStartEvent { AppMeta = _applicationMetaData });

            _papercutServer.BindObservable(
                PapercutClient.Localhost,
                PapercutClient.ServerPort,
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
                        "Unable to Create Papercut Server Listener on {IP}:{Port}. After 5 Retries. Failing",
                        PapercutClient.Localhost,
                        PapercutClient.ServerPort),
                    // on complete
                    () => { });

            _smtpServer.BindObservable(_serviceSettings.IP,_serviceSettings.Port, TaskPoolScheduler.Default)
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
                        new PapercutServiceReadyEvent { AppMeta = _applicationMetaData }));
        }

        public void Stop()
        {
            _papercutServer.Stop();
            _smtpServer.Stop();
            _messageBus.Publish(new PapercutServiceExitEvent { AppMeta = _applicationMetaData });
        }

        public void Dispose()
        {
            this._papercutServer?.Dispose();
            this._smtpServer?.Dispose();
        }
    }
}