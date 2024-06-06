// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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


using Autofac;
using Autofac.Util;

using Papercut.Common.Domain;
using Papercut.Core.Domain.Network;
using Papercut.Core.Domain.Network.Smtp;
using Papercut.Domain.Events;
using Papercut.Infrastructure.Smtp;

namespace Papercut.AppLayer.SmtpServers
{
    public class SmtpServerCoordinator : Disposable,
        IEventHandler<SettingsUpdatedEvent>, IEventHandler<PapercutServiceStatusEvent>
    {
        readonly ILogger _logger;

        readonly IMessageBus _messageBus;

        private readonly PapercutSmtpServer _smtpServer;

        public SmtpServerCoordinator(
            PapercutSmtpServer smtpServer,
            ILogger logger,
            IMessageBus messageBus)
        {
            this._smtpServer = smtpServer;
            this._logger = logger;
            this._messageBus = messageBus;
        }

        public bool IsServerActive => this._smtpServer.IsActive;

        public async Task HandleAsync(PapercutServiceStatusEvent @event, CancellationToken token = default)
        {
            if (@event.PapercutServiceStatus == PapercutServiceStatusType.Online)
            {
                this._logger.Information(
                    "Papercut Backend Service is running. SMTP disabled in UI.");

                if (this.IsServerActive) await this.StopServerAsync();
            }
            else if (@event.PapercutServiceStatus == PapercutServiceStatusType.Offline)
            {
                this._logger.Information(
                    "Papercut Backend Service is not running. SMTP enabled in UI.");

                // give it a half second though
                await Task.Delay(TimeSpan.FromMilliseconds(500), token);

                if (!this.IsServerActive) await this.ListenServerAsync();
            }
        }

        public async Task HandleAsync(SettingsUpdatedEvent @event, CancellationToken token)
        {
            if (!this.IsServerActive) return;

            if (@event.PreviousSettings.IP == @event.NewSettings.IP && @event.PreviousSettings.Port == @event.NewSettings.Port) return;

            await this.ListenServerAsync();
        }

        protected override async ValueTask DisposeAsync(bool disposing)
        {
            if (disposing)
            {
                if (this._smtpServer != null)
                {
                    await this.StopServerAsync();
                    await this._smtpServer.DisposeAsync();
                }
            }
        }

        private async Task StopServerAsync()
        {
            await this._smtpServer.StopAsync();
        }

        async Task ListenServerAsync()
        {
            try
            {
                await this._smtpServer.StopAsync();
                await this._smtpServer.StartAsync(new EndpointDefinition(Properties.Settings.Default.IP, Properties.Settings.Default.Port));
                await this._messageBus.PublishAsync(new SmtpServerBindEvent(Properties.Settings.Default.IP, Properties.Settings.Default.Port));
            }
            catch (Exception ex)
            {
                this._logger.Warning(
                    ex,
                    "Failed to bind SMTP to the {Address} {Port} specified. The port may already be in use by another process.",
                    Properties.Settings.Default.IP,
                    Properties.Settings.Default.Port);

                await this._messageBus.PublishAsync(new SmtpServerBindFailedEvent());
            }
        }

        #region Begin Static Container Registrations

        /// <summary>
        /// Called dynamically from the RegisterStaticMethods() call in the container module.
        /// </summary>
        /// <param name="builder"></param>
        [UsedImplicitly]
        static void Register(ContainerBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.RegisterType<SmtpServerCoordinator>().AsSelf()
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();
        }

        #endregion
    }
}