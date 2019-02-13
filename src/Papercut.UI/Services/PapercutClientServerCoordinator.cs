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
    using Papercut.Network;
    using Papercut.Network.Protocols;
    using Papercut.Network.Smtp;

    using Serilog;

    public class PapercutClientServerCoordinator : IEventHandler<PapercutClientReadyEvent>,
        IEventHandler<PapercutClientExitEvent>
    {
        readonly ILogger _logger;

        readonly IServer _papercutServer;

        public PapercutClientServerCoordinator(
            Func<ServerProtocolType, IServer> serverFactory,
            ILogger logger)
        {
            this._logger = logger;
            this._papercutServer = serverFactory(ServerProtocolType.PCComm);
        }

        public async Task Handle(PapercutClientExitEvent @event)
        {
            await Task.CompletedTask;

            this._papercutServer.Stop();
        }

        public async Task Handle(PapercutClientReadyEvent @event)
        {
            await Task.CompletedTask;

            this._papercutServer.BindObservable(
                PapercutClient.Localhost,
                PapercutClient.ClientPort,
                TaskPoolScheduler.Default)
                .DelaySubscription(TimeSpan.FromMilliseconds(500)).Retry(5)
                .Subscribe(
                    b => { },
                    ex => this._logger.Warning(
                        ex,
                        "Papercut Protocol failed to bind to the {Address} {Port} specified. The port may already be in use by another process.",
                        PapercutClient.Localhost,
                        PapercutClient.ClientPort),
                    () => { });
        }
    }
}