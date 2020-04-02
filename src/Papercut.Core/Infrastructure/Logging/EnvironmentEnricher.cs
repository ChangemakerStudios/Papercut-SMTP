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

namespace Papercut.Core.Infrastructure.Logging
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    using Serilog.Core;
    using Serilog.Events;

    public class EnvironmentEnricher : ILogEventEnricher
    {
        readonly ConcurrentDictionary<string, LogEventProperty> _cachedProperties =
            new ConcurrentDictionary<string, LogEventProperty>();

        /// <summary>
        /// Enrich the log event.
        /// 
        /// </summary>
        /// <param name="logEvent">The log event to enrich.</param><param name="propertyFactory">Factory for creating new properties to add to the event.</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var properties = new List<LogEventProperty>
            {
                this._cachedProperties.GetOrAdd("MachineName",
                    k => propertyFactory.CreateProperty(k, Environment.MachineName)),
                this._cachedProperties.GetOrAdd("Is64BitOperatingSystem",
                    k => propertyFactory.CreateProperty(k, Environment.Is64BitOperatingSystem)),
                this._cachedProperties.GetOrAdd("OSVersion", k => propertyFactory.CreateProperty(k, Environment.OSVersion)),
                this._cachedProperties.GetOrAdd("ProcessorCount",
                    k => propertyFactory.CreateProperty(k, Environment.ProcessorCount)),
                this._cachedProperties.GetOrAdd(".NETVersion", k => propertyFactory.CreateProperty(k, Environment.Version))
            };

            foreach (var p in properties)
            {
                logEvent.AddPropertyIfAbsent(p);
            }
        }
    }
}