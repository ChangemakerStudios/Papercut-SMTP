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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Autofac;

    using Papercut.Common.Extensions;

    using Serilog;

    public class RegisterPlugins
    {
        private readonly ILogger _logger;

        public RegisterPlugins(ILogger logger)
        {
            this._logger = logger.ForContext<RegisterPlugins>();
        }

        public void Register(ContainerBuilder builder, IEnumerable<Assembly> scannableAssemblies)
        {
            var pluginModules =
                scannableAssemblies.IfNullEmpty()
                    .SelectMany(a => a.GetExportedTypes())
                    .Where(
                        s =>
                        {
                            var interfaces = s.GetInterfaces();
                            return interfaces.Contains(typeof(IDiscoverableModule)) || interfaces.Contains(typeof(IPluginModule));
                        })
                    .ToList();

            var modules = pluginModules.Select(
                type =>
                {
                    try
                    {
                        return Activator.CreateInstance(type) as IDiscoverableModule;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Failure Loading Plugin Module Type {PluginModuleType}", type.FullName);
                    }

                    return null;
                }).Where(s => s != null).ToList();

            var plugins = modules.OfType<IPluginModule>().Distinct(PluginModuleEqualityComparer.Instance).ToList();

            foreach (var plugin in plugins)
            {
                builder.RegisterModule(plugin.Module);
                PluginStore.Instance.Add(plugin);
            }

            foreach (var plugin in modules.Except(plugins))
            {
                builder.RegisterModule(plugin.Module);
            }
        }
    }
}