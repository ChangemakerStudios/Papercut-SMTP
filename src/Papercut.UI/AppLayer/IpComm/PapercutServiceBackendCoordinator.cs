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


namespace Papercut.AppLayer.IpComm
{
    using System;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using Autofac;

    using Papercut.AppLayer.SmtpServers;
    using Papercut.Common.Domain;
    using Papercut.Core.Annotations;
    using Papercut.Core.Domain.Network.Smtp;
    using Papercut.Core.Domain.Rules;
    using Papercut.Core.Infrastructure.Lifecycle;
    using Papercut.Core.Infrastructure.Network;
    using Papercut.Domain.BackendService;
    using Papercut.Domain.Events;
    using Papercut.Infrastructure.IPComm;
    using Papercut.Infrastructure.IPComm.Network;
    using Papercut.Properties;

    using Serilog;

    public class PapercutServiceBackendCoordinator : IBackendServiceStatus,
        IEventHandler<SettingsUpdatedEvent>,
        IEventHandler<RulesUpdatedEvent>,
        IEventHandler<PapercutServicePreStartEvent>,
        IEventHandler<PapercutServiceReadyEvent>,
        IEventHandler<PapercutServiceExitEvent>
    {
        const string BackendServiceFailureMessage =
            "Papercut Backend Service Exception Attempting to Contact";

        readonly PapercutIPCommClientFactory _ipCommClientFactory;

        readonly ILogger _logger;

        readonly IMessageBus _messageBus;

        readonly SmtpServerCoordinator _smtpServerCoordinator;

        Action<RulesUpdatedEvent> _nextUpdateEvent;

        public PapercutServiceBackendCoordinator(
            ILogger logger,
            IMessageBus messageBus,
            PapercutIPCommClientFactory ipCommClientFactory,
            SmtpServerCoordinator smtpServerCoordinator)
        {
            this._logger = logger;
            this._messageBus = messageBus;
            this._ipCommClientFactory = ipCommClientFactory;
            this._smtpServerCoordinator = smtpServerCoordinator;

            IObservable<RulesUpdatedEvent> rulesUpdateObservable = Observable
                .Create<RulesUpdatedEvent>(
                    o =>
                    {
                        this._nextUpdateEvent = o.OnNext;
                        return Disposable.Empty;
                    }).SubscribeOn(TaskPoolScheduler.Default);

            // flush rules every 10 seconds
            rulesUpdateObservable.Buffer(TimeSpan.FromSeconds(10))
                .Where(e => e.Any())
                .Subscribe(async events => await this.PublishUpdateEvent(events.Last()));
        }

        public bool IsOnline { get; private set; }

        public Task HandleAsync(PapercutServiceExitEvent @event)
        {
            this.IsOnline = false;
            this._smtpServerCoordinator.SmtpServerEnabled = true;

            return Task.CompletedTask;
        }

        public Task HandleAsync(PapercutServicePreStartEvent @event)
        {
            this.IsOnline = true;
            this._smtpServerCoordinator.SmtpServerEnabled = false;

            return Task.CompletedTask;
        }

        public async Task HandleAsync(PapercutServiceReadyEvent @event)
        {
            await this.AttemptExchange();
        }

        public Task HandleAsync(RulesUpdatedEvent @event)
        {
            if (this.IsOnline)
            {
                this._nextUpdateEvent(@event);
            }

            return Task.CompletedTask;
        }

        public async Task HandleAsync(SettingsUpdatedEvent @event)
        {
            await this.PublishSmtpUpdated(@event);
        }

        public async Task PublishSmtpUpdated(SettingsUpdatedEvent @event)
        {
            if (!this.IsOnline) return;

            // check if the setting changed
            if (@event.PreviousSettings.IP == @event.NewSettings.IP && @event.PreviousSettings.Port == @event.NewSettings.Port) return;

            try
            {
                var messenger = this.GetClient();

                // update the backend service with the new ip/port settings...
                var smtpServerBindEvent = new SmtpServerBindEvent(
                    Settings.Default.IP,
                    Settings.Default.Port);

                bool successfulPublish = await messenger.PublishEventServer(smtpServerBindEvent);

                this._logger.Information(
                    successfulPublish
                        ? "Successfully pushed new Smtp Server Binding to Backend Service"
                        : "Papercut Backend Service Failed to Update. Could be offline.");
            }
            catch (Exception ex)
            {
                this._logger.Warning(ex, BackendServiceFailureMessage);
            }
        }

        private async Task AttemptExchange()
        {
            try
            {
                var sendEvent = new AppProcessExchangeEvent();

                // attempt to connect to the backend server...
                var ipCommClient = this.GetClient();

                var receivedEvent = await ipCommClient.ExchangeEventServer(sendEvent);

                if (receivedEvent == null) return;

                this.IsOnline = true;

                // backend server is online...
                this._logger.Information(
                    "Papercut Backend Service Running. Disabling SMTP in App.");
                this._smtpServerCoordinator.SmtpServerEnabled = false;

                if (!string.IsNullOrWhiteSpace(receivedEvent.MessageWritePath))
                {
                    this._logger.Debug(
                        "Background Process Returned {@Event} -- Publishing",
                        receivedEvent);

                    await this._messageBus.PublishAsync(receivedEvent);
                }
            }
            catch (Exception ex)
            {
                this._logger.Warning(ex, BackendServiceFailureMessage);
            }
        }

        async Task PublishUpdateEvent(RulesUpdatedEvent @event)
        {
            try
            {
                var ipCommClient = this.GetClient();

                bool successfulPublish = await ipCommClient.PublishEventServer(@event);

                this._logger.Information(
                    successfulPublish
                        ? "Successfully Updated Rules on Backend Service"
                        : "Papercut Backend Service Failed to Update Rules. Could be offline.");
            }
            catch (Exception ex)
            {
                this._logger.Warning(ex, BackendServiceFailureMessage);
            }
        }

        PapercutIPCommClient GetClient()
        {
            return this._ipCommClientFactory.GetClient(PapercutIPCommClientConnectTo.Service);
        }

        #region Begin Static Container Registrations

        /// <summary>
        /// Called dynamically from the RegisterStaticMethods() call in the container module.
        /// </summary>
        /// <param name="builder"></param>
        [UsedImplicitly]
        static void Register([NotNull] ContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.RegisterType<PapercutClientServerCoordinator>().AsImplementedInterfaces()
                .InstancePerLifetimeScope();
        }

        #endregion
    }
}