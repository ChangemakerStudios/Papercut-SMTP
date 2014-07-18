// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2014 Jaben Cargman
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

    using Papercut.Core.Events;
    using Papercut.Core.Network;
    using Papercut.Events;
    using Papercut.Properties;

    using Serilog;

    public class PapercutServiceBackendCoordinator : IHandleEvent<AppPreStartEvent>,
        IHandleEvent<SettingsUpdatedEvent>,
        IHandleEvent<RulesUpdatedEvent>
    {
        readonly ILogger _logger;

        readonly Func<PapercutClient> _papercutClientFactory;

        readonly IPublishEvent _publishEvent;

        readonly SmtpServerCoordinator _smtpServerCoordinator;

        Action<RulesUpdatedEvent> _nextUpdateEvent;

        IObservable<RulesUpdatedEvent> rulesUpdateObservable;

        public PapercutServiceBackendCoordinator(
            ILogger logger,
            IPublishEvent publishEvent,
            Func<PapercutClient> papercutClientFactory,
            SmtpServerCoordinator smtpServerCoordinator)
        {
            _logger = logger;
            _publishEvent = publishEvent;
            _papercutClientFactory = papercutClientFactory;
            _smtpServerCoordinator = smtpServerCoordinator;

            rulesUpdateObservable = Observable.Create<RulesUpdatedEvent>(
                o =>
                {
                    _nextUpdateEvent = o.OnNext;
                    return Disposable.Empty;
                }).SubscribeOn(TaskPoolScheduler.Default);

            // flush rules every 10 seconds
            rulesUpdateObservable.Buffer(TimeSpan.FromSeconds(10))
                .Where(e => e.Any())
                .Subscribe(events => PublishUpdateEvent(events.Last()));
        }

        public bool IsBackendServiceOnline { get; private set; }

        public void Handle(AppPreStartEvent @event)
        {
            try
            {
                var exchangeEvent = new AppProcessExchangeEvent();

                // attempt to connect to the backend server...
                using (PapercutClient client = GetClient())
                {
                    if (!client.ExchangeEventServer(ref exchangeEvent)) return;

                    IsBackendServiceOnline = true;

                    // backend server is online...
                    _logger.Information("Papercut Backend Service Running. Disabling SMTP in App.");
                    _smtpServerCoordinator.SmtpServerEnabled = false;

                    if (!string.IsNullOrWhiteSpace(exchangeEvent.MessageWritePath))
                    {
                        _logger.Debug(
                            "Background Process Returned {@Event} -- Publishing",
                            exchangeEvent);

                        _publishEvent.Publish(exchangeEvent);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Papercut Backend Service Exception Attempting to Contact");
            }
        }

        public void Handle(RulesUpdatedEvent @event)
        {
            if (!IsBackendServiceOnline) return;

            _nextUpdateEvent(@event);
        }

        public void Handle(SettingsUpdatedEvent @event)
        {
            if (!IsBackendServiceOnline) return;

            try
            {
                using (PapercutClient client = GetClient())
                {
                    // update the backend service with the new ip/port settings...
                    var smtpServerBindEvent = new SmtpServerBindEvent(
                        Settings.Default.IP,
                        Settings.Default.Port);

                    bool successfulPublish =
                        client.PublishEventServer(smtpServerBindEvent);

                    _logger.Information(
                        successfulPublish
                            ? "Successfully pushed new Smtp Server Binding to Backend Service"
                            : "Papercut Backend Service Failed to Update. Could be offline.");
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Papercut Backend Service Exception Attempting to Contact");
            }
        }

        void PublishUpdateEvent(RulesUpdatedEvent @event)
        {
            try
            {
                using (PapercutClient client = GetClient())
                {
                    bool successfulPublish =
                        client.PublishEventServer(@event);

                    _logger.Information(
                        successfulPublish
                            ? "Successfully Updated Rules on Backend Service"
                            : "Papercut Backend Service Failed to Update Rules. Could be offline.");
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Papercut Backend Service Exception Attempting to Contact");
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