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

namespace Papercut.Core.Infrastructure.Plugins
{
    using System.Threading;
    using System.Threading.Tasks;

    using Autofac;

    using Papercut.Common.Domain;
    using Papercut.Core.Infrastructure.Lifecycle;

    using Serilog;

    public class PluginReport : IEventHandler<PapercutClientReadyEvent>, IEventHandler<PapercutServiceReadyEvent>
    {
        private readonly IPluginStore _pluginStore;
        private readonly ILifetimeScope _scope;

        public PluginReport(IPluginStore pluginStore, ILifetimeScope scope)
        {
            this._pluginStore = pluginStore;
            this._scope = scope;
        }

        private void PluginInfoDump()
        {
            var logger = this._scope.Resolve<ILogger>().ForContext<PluginReport>();

            foreach (var pluginModule in this._pluginStore.Plugins)
            {
                logger.Information(
                    "Running Plug-In {PluginName} {PluginVersion} {PluginDescription}",
                    pluginModule.Name,
                    pluginModule.Version,
                    pluginModule.Description);
            }
        }

        public void Handle(PapercutClientReadyEvent @event)
        {
            this.PluginInfoDump();
        }

        public void Handle(PapercutServiceReadyEvent @event)
        {
            this.PluginInfoDump();
        }
    }
}