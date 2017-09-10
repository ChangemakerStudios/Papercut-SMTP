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

namespace Papercut.Module.VerboseJsonLog
{
    using System;
    using System.IO;

    using Papercut.Common.Domain;
    using Papercut.Core.Domain.Application;
    using Papercut.Core.Infrastructure.Logging;

    using Serilog.Events;
    using Serilog.Formatting.Json;
    using Serilog.Sinks.RollingFile;

    public class SetupVerboseJsonLoggingHandler : IEventHandler<ConfigureLoggerEvent>
    {
        readonly IAppMeta _appMeta;

        public SetupVerboseJsonLoggingHandler(IAppMeta appMeta)
        {
            _appMeta = appMeta;
        }

        public void Handle(ConfigureLoggerEvent @event)
        {
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Logs",
                $"{this._appMeta.AppName}.json");

            var jsonSink = new RollingFileSink(logFilePath, new JsonFormatter(), null, null);
            @event.LogConfiguration.MinimumLevel.Debug().WriteTo.Sink(jsonSink, LogEventLevel.Debug);
        }
    }
}