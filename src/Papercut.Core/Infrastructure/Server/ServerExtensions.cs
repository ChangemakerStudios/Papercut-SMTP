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
namespace Papercut.Core.Infrastructure.Server
{
    using System;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using Papercut.Core.Annotations;
    using Papercut.Core.Domain.Network;

    public static class ServerExtensions
    {
        public static IObservable<bool> ObserveStartServer(
            [NotNull] this IServer server,
            string ip,
            int port,
            IScheduler scheduler = null)
        {
            return server.ObserveStartServer(new EndpointDefinition(ip, port), scheduler);
        }

        public static IObservable<bool> ObserveStartServer(
            [NotNull] this IServer server,
            EndpointDefinition endpoint,
            IScheduler scheduler = null)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));

            IObservable<bool> bindObservable = Observable.Create(
                 (IObserver<bool> o) =>
                {
                    Observer.Synchronize(o);
                    try
                    {
                        server.Stop();

                        server.Start(endpoint);

                        o.OnCompleted();
                    }
                    catch (Exception ex)
                    {
                        o.OnError(ex);
                    }

                    return Disposable.Empty;
                }).ObserveOn(scheduler ?? Scheduler.Default);

            return bindObservable;
        }
    }
}