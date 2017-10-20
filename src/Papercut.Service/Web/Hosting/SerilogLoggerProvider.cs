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


namespace Papercut.Service.Web.Hosting
{
    using System;
    using Microsoft.Extensions.Logging;
    using Serilog.Events;
    using System.Collections.Generic;
    using Autofac;

    class SerilogLoggerProvider : ILoggerProvider
    {
        ILifetimeScope _scope;
        public SerilogLoggerProvider(ILifetimeScope scope)
        {
            this._scope = scope;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new SerilogLoggerAdapter(_scope.Resolve<Serilog.ILogger>());
        }

        public void Dispose()
        {

        }
    }

    class SerilogLoggerAdapter : ILogger, IDisposable
    {
        private Serilog.ILogger logger;

        public SerilogLoggerAdapter(Serilog.ILogger logger)
        {
            this.logger = logger;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            if (logLevel == LogLevel.None)
            {
                return false;
            }

            var serilogLevel = levelMapping[logLevel];
            return logger.IsEnabled(serilogLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel == LogLevel.None || !IsEnabled(logLevel))
            {
                return;
            }

            var serilogLevel = levelMapping[logLevel];
            var logString = formatter(state, exception);
            logger.Write(serilogLevel, exception, logString);
        }

        static Dictionary<LogLevel, LogEventLevel> levelMapping = new Dictionary<LogLevel, LogEventLevel>
                {
                    { LogLevel.Critical, LogEventLevel.Fatal },
                    { LogLevel.Error, LogEventLevel.Error},
                    { LogLevel.Warning, LogEventLevel.Warning},
                    { LogLevel.Information, LogEventLevel.Information},
                    { LogLevel.Debug, LogEventLevel.Debug},
                    { LogLevel.Trace, LogEventLevel.Verbose },
                };

        void IDisposable.Dispose()
        {

        }
    }
}
