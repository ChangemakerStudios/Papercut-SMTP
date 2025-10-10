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


using Papercut.Core.Domain.Network;
using Papercut.Core.Domain.Network.Smtp;
using Papercut.Core.Domain.Settings;

namespace Papercut.Service.Infrastructure.Servers
{
    public class SmtpServerManager : IEventHandler<SmtpServerBindEvent>, IEventHandler<PapercutServiceReadyEvent>
    {
        private readonly ILogger _logger;

        private readonly PapercutSmtpServer _smtpServer;

        private readonly SmtpServerOptions _smtpServerOptions;

        private readonly ISettingStore _settingStore;

        public SmtpServerManager(PapercutSmtpServer smtpServer,
            SmtpServerOptions smtpServerOptions,
            ISettingStore settingStore,
            ILogger logger)
        {
            this._smtpServer = smtpServer;
            this._smtpServerOptions = smtpServerOptions;
            this._settingStore = settingStore;
            this._logger = logger;
        }

        public async Task HandleAsync(PapercutServiceReadyEvent @event, CancellationToken token = default)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), token);
            await this.BindSMTPServer();
        }

        public async Task HandleAsync(SmtpServerBindEvent @event, CancellationToken token = default)
        {
            this._logger.Information(
                "Received New Smtp Server Binding Settings from UI {@Event}",
                @event);

            // update settings...
            this._smtpServerOptions.IP = @event.IP;
            this._smtpServerOptions.Port = @event.Port;

            // persist the settings to Settings.json so they survive restarts
            try
            {
                this._settingStore.Set("IP", @event.IP);
                this._settingStore.Set("Port", @event.Port.ToString());
                this._settingStore.Save();

                this._logger.Information(
                    "Persisted SMTP Server settings: IP={IP}, Port={Port}",
                    @event.IP,
                    @event.Port);
            }
            catch (Exception ex)
            {
                this._logger.Warning(ex, "Failed to persist SMTP server settings");
            }

            // rebind the server...
            await this.BindSMTPServer();
        }

        async Task BindSMTPServer()
        {
            try
            {
                await this._smtpServer.StopAsync();
                await this._smtpServer.StartAsync(
                    new EndpointDefinition(this._smtpServerOptions.IP, this._smtpServerOptions.Port));
            }
            catch (Exception ex)
            {
                this._logger.Warning(
                    ex,
                    "Unable to Create SMTP Server Listener on {IP}:{Port}. After 5 Retries. Failing",
                    this._smtpServerOptions.IP,
                    this._smtpServerOptions.Port);
            }
        }

        #region Begin Static Container Registrations

        static void Register(ContainerBuilder builder)
        {
            builder.RegisterType<SmtpServerManager>().AsImplementedInterfaces().AsSelf()
                .InstancePerLifetimeScope();
        }

        #endregion
    }
}