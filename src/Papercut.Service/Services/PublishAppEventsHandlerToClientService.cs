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

    using Common.Domain;

    using Core.Infrastructure.Lifecycle;

    using Infrastructure.IPComm;
    using Infrastructure.IPComm.Network;

    using Serilog;

    public class PublishAppEventsHandlerToClientService : IEventHandler<PapercutServiceExitEvent>,
        IEventHandler<PapercutServiceReadyEvent>,
        IEventHandler<PapercutServicePreStartEvent>
    {
        readonly ILogger _logger;

        readonly PapercutIPCommClientFactory _ipCommClientFactory;

        public PublishAppEventsHandlerToClientService(
            PapercutIPCommClientFactory ipCommClientFactory,
            ILogger logger)
        {
            _ipCommClientFactory = ipCommClientFactory;
            _logger = logger;
        }

        public void Handle(PapercutServiceExitEvent @event)
        {
            Publish(@event);
        }

        public void Handle(PapercutServicePreStartEvent @event)
        {
            Publish(@event);
        }

        public void Handle(PapercutServiceReadyEvent @event)
        {
            Publish(@event);
        }

        public void Publish<T>(T @event)
            where T : IEvent
        {
            using (var ipCommClient = _ipCommClientFactory.GetClient(PapercutIPCommClientConnectTo.UI))
            {
                try
                {
                    _logger.Information(
                        $"Publishing {{@{@event.GetType().Name}}} to the Papercut Client",
                        @event);

                    ipCommClient.PublishEventServer(@event);
                }

                catch (Exception ex)
                {
                    _logger.Warning(
                        ex,
                        "Failed to publish {Endpoint} specified. Papercut UI is most likely not running.",
                        ipCommClient.Endpoint);
                }
            }
        }
    }
}