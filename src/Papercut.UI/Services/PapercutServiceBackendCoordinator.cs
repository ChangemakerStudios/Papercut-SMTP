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

namespace Papercut.Services
{
    using System;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using Papercut.Common.Domain;
    using Papercut.Core.Domain.Network.Smtp;
    using Papercut.Core.Domain.Rules;
    using Papercut.Core.Infrastructure.Lifecycle;
    using Papercut.Core.Infrastructure.Network;
    using Papercut.Events;
    using Papercut.Network;
    using Papercut.Properties;

    using Serilog;

    public class PapercutServiceBackendCoordinator : IEventHandler<PapercutClientPreStartEvent>,
        IEventHandler<SettingsUpdatedEvent>,
        IEventHandler<RulesUpdatedEvent>,
        IEventHandler<PapercutServicePreStartEvent>,
        IEventHandler<PapercutServiceReadyEvent>,
        IEventHandler<PapercutServiceExitEvent>
    {
        const string BackendServiceFailureMessage =
            "Papercut Backend Service Exception Attempting to Contact";

        readonly ILogger _logger;

        readonly Func<PapercutClient> _papercutClientFactory;

        readonly IMessageBus _messageBus;

        readonly SmtpServerCoordinator _smtpServerCoordinator;

        Action<RulesUpdatedEvent> _nextUpdateEvent;

        public PapercutServiceBackendCoordinator(
            ILogger logger,
            IMessageBus messageBus,
            Func<PapercutClient> papercutClientFactory,
            SmtpServerCoordinator smtpServerCoordinator)
        {
            _logger = logger;
            this._messageBus = messageBus;
            _papercutClientFactory = papercutClientFactory;
            _smtpServerCoordinator = smtpServerCoordinator;

            IObservable<RulesUpdatedEvent> rulesUpdateObservable = Observable
                .Create<RulesUpdatedEvent>(
                    o =>
                    {
                        _nextUpdateEvent = o.OnNext;
                        return Disposable.Empty;
                    }).SubscribeOn(TaskPoolScheduler.Default);

            // flush rules every 10 seconds
            rulesUpdateObservable.Buffer(TimeSpan.FromSeconds(10))
                .Where(e => e.Any())
                .Subscribe(async events => await PublishUpdateEvent(events.Last()));
        }

        public bool IsBackendServiceOnline { get; private set; }

        public async Task Handle(PapercutClientPreStartEvent @event)
        {
            await this.DoProcessExchange();
        }

        public async Task Handle(PapercutServiceExitEvent @event)
        {
            await Task.CompletedTask;

            IsBackendServiceOnline = false;
            _smtpServerCoordinator.SmtpServerEnabled = true;
        }

        public async Task Handle(PapercutServicePreStartEvent @event)
        {
            await Task.CompletedTask;

            IsBackendServiceOnline = true;
            _smtpServerCoordinator.SmtpServerEnabled = false;
        }

        public async Task Handle(PapercutServiceReadyEvent @event)
        {
            await this.DoProcessExchange();
        }

        public async Task Handle(RulesUpdatedEvent @event)
        {
            await Task.CompletedTask;

            if (!IsBackendServiceOnline) return;

            _nextUpdateEvent(@event);
        }

        public async Task Handle(SettingsUpdatedEvent @event)
        {
            await Task.CompletedTask;

            if (!IsBackendServiceOnline) return;

            // check if the setting changed
            if (@event.PreviousSettings.IP == @event.NewSettings.IP && @event.PreviousSettings.Port == @event.NewSettings.Port) return;

            try
            {
                using (PapercutClient client = GetClient())
                {
                    // update the backend service with the new ip/port settings...
                    var smtpServerBindEvent = new SmtpServerBindEvent(
                        Settings.Default.IP,
                        Settings.Default.Port);

                    bool successfulPublish = await client.PublishEventServer(smtpServerBindEvent);

                    _logger.Information(
                        successfulPublish
                            ? "Successfully pushed new Smtp Server Binding to Backend Service"
                            : "Papercut Backend Service Failed to Update. Could be offline.");
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, BackendServiceFailureMessage);
            }
        }

        async Task DoProcessExchange()
        {
            try
            {
                var sendEvent = new AppProcessExchangeEvent();

                // attempt to connect to the backend server...
                using (PapercutClient client = GetClient())
                {
                    var receivedEvent = await client.ExchangeEventServer(sendEvent);
                    
                    if (receivedEvent == null) return;

                    IsBackendServiceOnline = true;

                    // backend server is online...
                    _logger.Information("Papercut Backend Service Running. Disabling SMTP in App.");
                    _smtpServerCoordinator.SmtpServerEnabled = false;

                    if (!string.IsNullOrWhiteSpace(receivedEvent.MessageWritePath))
                    {
                        _logger.Debug(
                            "Background Process Returned {@Event} -- Publishing",
                            receivedEvent);

                        await this._messageBus.Publish(receivedEvent);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, BackendServiceFailureMessage);
            }
        }

        async Task PublishUpdateEvent(RulesUpdatedEvent @event)
        {
            try
            {
                using (PapercutClient client = GetClient())
                {
                    bool successfulPublish = await client.PublishEventServer(@event);

                    _logger.Information(
                        successfulPublish
                            ? "Successfully Updated Rules on Backend Service"
                            : "Papercut Backend Service Failed to Update Rules. Could be offline.");
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, BackendServiceFailureMessage);
            }
        }

        PapercutClient GetClient()
        {
            PapercutClient client = _papercutClientFactory();
            client.Port = PapercutClient.ServerPort;
            return client;
        }
    }
}