// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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


using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

using Autofac;

using Serilog.Configuration;
using Serilog.Events;

namespace Papercut.AppLayer.LogSinks
{
    public class UiLogSinkQueue : ILoggerSettings
    {
        static readonly ConcurrentQueue<LogEvent> _logQueue = new ConcurrentQueue<LogEvent>();

        public void Configure(LoggerConfiguration loggerConfiguration)
        {
            bool showDebug = false;
#if DEBUG
            showDebug = true;
#endif
            loggerConfiguration.WriteTo.Observers(
                b => b.ObserveOn(TaskPoolScheduler.Default).Subscribe(le =>
                {
                    _logQueue.Enqueue(le);
                    this.OnLogEvent();
                }),
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                showDebug ? LogEventLevel.Debug : LogEventLevel.Information);
        }

        public LogEvent? GetLastEvent()
        {
            return _logQueue.TryDequeue(out var log) ? log : null;
        }

        public IEnumerable<LogEvent> GetLastEvents()
        {
            while (this.GetLastEvent() is { } logEvent)
            {
               yield return logEvent;
            }
        }

        public event EventHandler LogEvent;

        protected virtual void OnLogEvent()
        {
            this.LogEvent?.Invoke(this, EventArgs.Empty);
        }

        #region Begin Static Container Registrations

        /// <summary>
        /// Called dynamically from the RegisterStaticMethods() call in the container module.
        /// </summary>
        /// <param name="builder"></param>
        [UsedImplicitly]
        static void Register(ContainerBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.RegisterType<UiLogSinkQueue>().As<ILoggerSettings>().AsSelf();
        }

        #endregion
    }
}