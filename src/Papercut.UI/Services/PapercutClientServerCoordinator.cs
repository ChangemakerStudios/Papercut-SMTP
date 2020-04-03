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


namespace Papercut.Services
{
    using System;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;

    using Common.Domain;

    using Core.Infrastructure.Lifecycle;
    using Core.Infrastructure.Server;

    using Infrastructure.IPComm.Network;

    using Serilog;

    public class PapercutClientServerCoordinator : IEventHandler<PapercutClientReadyEvent>,
        IEventHandler<PapercutClientExitEvent>
    {
        private readonly PapercutIPCommEndpoints _papercutIpCommEndpoints;

        readonly ILogger _logger;

        readonly PapercutIPCommServer _papercutIpCommServer;

        public PapercutClientServerCoordinator(
            PapercutIPCommEndpoints papercutIpCommEndpoints,
            PapercutIPCommServer ipCommServer,
            ILogger logger)
        {
            _papercutIpCommEndpoints = papercutIpCommEndpoints;
            _logger = logger;
            _papercutIpCommServer = ipCommServer;
        }

        public void Handle(PapercutClientExitEvent @event)
        {
            _papercutIpCommServer.Stop();
        }

        public void Handle(PapercutClientReadyEvent @event)
        {
            _papercutIpCommServer.ObserveStartServer(_papercutIpCommEndpoints.UI,
                    TaskPoolScheduler.Default)
                .DelaySubscription(TimeSpan.FromMilliseconds(500)).Retry(5)
                .Subscribe(
                    b => { },
                    ex => _logger.Warning(
                        ex,
                        "Papercut IPComm Server failed to bind to the {Address} {Port} specified. The port may already be in use by another process.",
                        _papercutIpCommServer.ListenIpAddress,
                        _papercutIpCommServer.ListenPort),
                    () => { });
        }
    }
}