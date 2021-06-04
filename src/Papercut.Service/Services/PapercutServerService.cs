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


namespace Papercut.Service.Services
{
    using System;
    using System.Threading.Tasks;

    using Papercut.Common.Domain;
    using Papercut.Core.Domain.Application;
    using Papercut.Core.Domain.Network;
    using Papercut.Core.Domain.Network.Smtp;
    using Papercut.Core.Domain.Settings;
    using Papercut.Core.Infrastructure.Lifecycle;
    using Papercut.Infrastructure.IPComm.Network;
    using Papercut.Infrastructure.Smtp;
    using Papercut.Service.Helpers;

    using Serilog;

    public class PapercutServerService : IEventHandler<SmtpServerBindEvent>
    {
        readonly IAppMeta _applicationMetaData;

        readonly PapercutIPCommServer _ipCommServer;

        readonly ILogger _logger;

        readonly IMessageBus _messageBus;

        private readonly PapercutIPCommEndpoints _papercutIpCommEndpoints;

        readonly PapercutServiceSettings _serviceSettings;

        private readonly PapercutSmtpServer _smtpServer;

        public PapercutServerService(
            PapercutIPCommServer ipCommServer,
            PapercutSmtpServer smtpServer,
            PapercutServiceSettings serviceSettings,
            PapercutIPCommEndpoints papercutIpCommEndpoints,
            IAppMeta applicationMetaData,
            ILogger logger,
            IMessageBus messageBus)
        {
            this._smtpServer = smtpServer;
            this._serviceSettings = serviceSettings;
            this._papercutIpCommEndpoints = papercutIpCommEndpoints;
            this._applicationMetaData = applicationMetaData;
            this._logger = logger;
            this._messageBus = messageBus;
            this._ipCommServer = ipCommServer;
        }

        public async Task HandleAsync(SmtpServerBindEvent @event)
        {
            this._logger.Information(
                "Received New Smtp Server Binding Settings from UI {@Event}",
                @event);

            // update settings...
            this._serviceSettings.IP = @event.IP;
            this._serviceSettings.Port = @event.Port;
            this._serviceSettings.Save();

            // rebind the server...
            await this.BindSMTPServer();
        }

        async Task BindSMTPServer()
        {
            try
            {
                await this._smtpServer.StopAsync();
                await this._smtpServer.StartAsync(
                    new EndpointDefinition(this._serviceSettings.IP, this._serviceSettings.Port));
            }
            catch (Exception ex)
            {
                this._logger.Warning(
                    ex,
                    "Unable to Create SMTP Server Listener on {IP}:{Port}. After 5 Retries. Failing",
                    this._serviceSettings.IP,
                    this._serviceSettings.Port);
            }
        }

        public async Task Start()
        {
            await this._messageBus.PublishAsync(
                new PapercutServicePreStartEvent { AppMeta = this._applicationMetaData });

            try
            {
                await this._ipCommServer.StopAsync();
                await this._ipCommServer.StartAsync(this._papercutIpCommEndpoints.Service);
            }
            catch (Exception ex)
            {
                this._logger.Warning(
                    ex,
                    "Unable to Create Papercut IPComm Server Listener on {IP}:{Port}. After 5 Retries. Failing",
                    this._ipCommServer.ListenIpAddress,
                    this._ipCommServer.ListenPort);
            }

            await this.BindSMTPServer();
            await this._messageBus.PublishAsync(
                new PapercutServiceReadyEvent { AppMeta = this._applicationMetaData });
        }

        public async Task Stop()
        {
            await this._ipCommServer.StopAsync();
            await this._messageBus.PublishAsync(new PapercutServiceExitEvent { AppMeta = this._applicationMetaData });
        }
    }
}