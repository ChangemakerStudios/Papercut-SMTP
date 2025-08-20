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


namespace Papercut.Service.Infrastructure.Servers
{
    using Core.Domain.Network;
    using Core.Domain.Network.Smtp;

    public class SmtpServerManager : IEventHandler<SmtpServerBindEvent>, IEventHandler<PapercutServiceReadyEvent>
    {
        private readonly ILogger _logger;

        private readonly PapercutSmtpServer _smtpServer;

        private readonly SmtpServerOptions _smtpServerOptions;

        public SmtpServerManager(PapercutSmtpServer smtpServer,
            SmtpServerOptions smtpServerOptions,
            ILogger logger)
        {
            _smtpServer = smtpServer;
            _smtpServerOptions = smtpServerOptions;
            _logger = logger;
        }

        public async Task HandleAsync(PapercutServiceReadyEvent @event, CancellationToken token = default)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), token);
            await BindSMTPServer();
        }

        public async Task HandleAsync(SmtpServerBindEvent @event, CancellationToken token = default)
        {
            _logger.Information(
                "Received New Smtp Server Binding Settings from UI {@Event}",
                @event);

            // update settings...
            _smtpServerOptions.IP = @event.IP;
            _smtpServerOptions.Port = @event.Port;
            //this._smtpServerOptions.Save();

            // rebind the server...
            await BindSMTPServer();
        }

        async Task BindSMTPServer()
        {
            try
            {
                await _smtpServer.StopAsync();
                await _smtpServer.StartAsync(
                    new EndpointDefinition(_smtpServerOptions.IP, _smtpServerOptions.Port));
            }
            catch (Exception ex)
            {
                _logger.Warning(
                    ex,
                    "Unable to Create SMTP Server Listener on {IP}:{Port}. After 5 Retries. Failing",
                    _smtpServerOptions.IP,
                    _smtpServerOptions.Port);
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