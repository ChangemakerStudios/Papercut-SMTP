// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2016 Jaben Cargman
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

namespace Papercut.Core.Infrastructure.Plugins
{
    using Autofac;

    using Serilog;

    public class PluginReport : IStartable
    {
        private readonly ILogger _logger;
        private readonly IPluginStore _pluginStore;

        public PluginReport(ILogger logger, IPluginStore pluginStore)
        {
            this._logger = logger;
            this._pluginStore = pluginStore;
        }

        public void Start()
        {
            foreach (var pluginModule in this._pluginStore.Plugins)
            {
                this._logger.Information("Loaded Plug-In {PluginName} {PluginVersion} {PluginDescription}", pluginModule.Name,
                    pluginModule.Version, pluginModule.Description);
            }
        }
    }
}