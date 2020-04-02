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

namespace Papercut.Helpers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using Papercut.Common.Domain;
    using Papercut.Core.Infrastructure.Logging;

    using Serilog;
    using Serilog.Events;

    public class LogClientSinkQueue : IEventHandler<ConfigureLoggerEvent>
    {
        static readonly ConcurrentQueue<LogEvent> _logQueue = new ConcurrentQueue<LogEvent>();

        public void Handle(ConfigureLoggerEvent @event)
        {
            bool showDebug = false;
#if DEBUG
            showDebug = true;
#endif
            @event.LogConfiguration.WriteTo.Observers(
                b => b.ObserveOn(TaskPoolScheduler.Default).Subscribe(le =>
                {
                    _logQueue.Enqueue(le);
                    OnLogEvent();
                }),
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                showDebug ? LogEventLevel.Debug : LogEventLevel.Information);
        }

        public LogEvent GetLastEvent()
        {
            return _logQueue.TryDequeue(out var log) ? log : null;
        }

        public IEnumerable<LogEvent> GetLastEvents()
        {
            LogEvent logEvent;

            while ((logEvent = GetLastEvent()) != null)
            {
               yield return logEvent;
            }
        }

        public event EventHandler LogEvent;

        protected virtual void OnLogEvent()
        {
            LogEvent?.Invoke(this, EventArgs.Empty);
        }
    }
}