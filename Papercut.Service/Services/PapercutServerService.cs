// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2015 Jaben Cargman
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
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using Papercut.Core.Configuration;
    using Papercut.Core.Events;
    using Papercut.Core.Network;
    using Papercut.Core.Settings;
    using Papercut.Service.Helpers;

    using Serilog;

    public class PapercutServerService : IHandleEvent<SmtpServerBindEvent>
    {
        readonly IAppMeta _applicationMetaData;

        readonly ILogger _logger;

        readonly IServer _papercutServer;

        readonly IPublishEvent _publishEvent;

        readonly PapercutServiceSettings _serviceSettings;

        readonly IServer _smtpServer;

        public PapercutServerService(
            Func<ServerProtocolType, IServer> serverFactory,
            PapercutServiceSettings serviceSettings,
            IAppMeta applicationMetaData,
            ILogger logger,
            IPublishEvent publishEvent)
        {
            _serviceSettings = serviceSettings;
            _applicationMetaData = applicationMetaData;
            _logger = logger;
            _publishEvent = publishEvent;
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
            _publishEvent.Publish(
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
                    _publishEvent.Publish(
                        new PapercutServiceReadyEvent { AppMeta = _applicationMetaData }));
        }

        public void Stop()
        {
            _smtpServer.Stop();
            _papercutServer.Stop();
            _publishEvent.Publish(new PapercutServiceExitEvent { AppMeta = _applicationMetaData });
        }
    }
}