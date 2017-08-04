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

namespace Papercut.Core.Infrastructure.MessageBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Autofac;

    using Papercut.Common.Domain;

    using Serilog;

    public class AutofacMessageBus : IMessageBus
    {
        readonly ILifetimeScope _lifetimeScope;

        public AutofacMessageBus(ILifetimeScope lifetimeScope)
        {
            this._lifetimeScope = lifetimeScope;
        }

        public void Publish<T>(T eventObject) where T : IEvent
        {
            foreach (var @event in this.MaybeByOrderable(this._lifetimeScope.Resolve<IEnumerable<IEventHandler<T>>>()))
            {
                try
                {
                    @event.Handle(eventObject);
                }
                catch (Exception ex)
                {
                    this._lifetimeScope.Resolve<ILogger>().ForContext<AutofacMessageBus>().Error(
                        ex,
                        "Failed publishing {EventType} to {EventHandler}",
                        typeof(T),
                        @event.GetType());
                }
            }
        }

        private List<T> MaybeByOrderable<T>(IEnumerable<T> @events)
        {
            return @events.Distinct()
                .Select((e, i) => new { Index = 100 + i, Event = e }).OrderBy(
                    e =>
                    {
                        var orderable = e.Event as IOrderable;
                        return orderable?.Order ?? e.Index;
                    }).Select(e => e.Event).ToList();
        }
    }
}