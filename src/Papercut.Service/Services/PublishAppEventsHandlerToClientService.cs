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
    using System.Threading.Tasks;

    using Papercut.Common.Domain;
    using Papercut.Core.Infrastructure.Lifecycle;
    using Papercut.Infrastructure.IPComm.IPComm;

    using Serilog;

    public class PublishAppEventsHandlerToClientService : IEventHandler<PapercutServiceExitEvent>,
        IEventHandler<PapercutServiceReadyEvent>,
        IEventHandler<PapercutServicePreStartEvent>
    {
        readonly ILogger _logger;

        readonly Func<PapercutIPCommClient> _papercutClientFactory;

        public PublishAppEventsHandlerToClientService(
            Func<PapercutIPCommClient> papercutClientFactory,
            ILogger logger)
        {
            _papercutClientFactory = papercutClientFactory;
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

        PapercutIPCommClient GetClient()
        {
            PapercutIPCommClient messenger = _papercutClientFactory();
            messenger.Port = IPCommConstants.UiListeningPort;
            return messenger;
        }

        public void Publish<T>(T @event)
            where T : IEvent
        {
            try
            {
                _logger.Information(
                    "Publishing {@" + @event.GetType().Name + "} to the Papercut Client",
                    @event);

                using (var ipCommClient = GetClient())
                {
                    ipCommClient.PublishEventServer(@event);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(
                    ex,
                    "Failed to publish {Address} {Port} specified. Papercut UI is most likely not running.",
                    IPCommConstants.Localhost,
                    IPCommConstants.UiListeningPort);
            }
        }
    }
}