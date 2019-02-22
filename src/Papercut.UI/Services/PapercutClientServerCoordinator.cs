// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2019 Jaben Cargman
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
    using System.Threading.Tasks;

    using Papercut.Common.Domain;
    using Papercut.Core.Domain.Network;
    using Papercut.Core.Infrastructure.Lifecycle;
    using Papercut.Core.Infrastructure.Server;
    using Papercut.Infrastructure.IPComm.IPComm;

    using Serilog;

    public class PapercutClientServerCoordinator : IEventHandler<PapercutClientReadyEvent>,
        IEventHandler<PapercutClientExitEvent>
    {
        readonly ILogger _logger;

        readonly PapercutIPCommServer _papercutIPCommServer;

        public PapercutClientServerCoordinator(
            PapercutIPCommServer ipCommServer,
            ILogger logger)
        {
            this._logger = logger;
            this._papercutIPCommServer = ipCommServer;
        }

        public void Handle(PapercutClientExitEvent @event)
        {
            this._papercutIPCommServer.Stop();
        }

        public void Handle(PapercutClientReadyEvent @event)
        {
            this._papercutIPCommServer.ObserveStartServer(
                    IPCommConstants.Localhost,
                    IPCommConstants.UiListeningPort,
                    TaskPoolScheduler.Default)
                .DelaySubscription(TimeSpan.FromMilliseconds(500)).Retry(5)
                .Subscribe(
                    b => { },
                    ex => this._logger.Warning(
                        ex,
                        "Papercut IPComm Server failed to bind to the {Address} {Port} specified. The port may already be in use by another process.",
                        IPCommConstants.Localhost,
                        IPCommConstants.UiListeningPort),
                    () => { });
        }
    }
}