// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2015 Jaben Cargman
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

namespace Papercut.Service
{
    using System.Collections.Generic;
    using System.Reflection;
    using Autofac;
    using Autofac.Core;
    using Papercut.Core.Configuration;
    using Papercut.Core.Helper;
    using Papercut.Core.Plugins;
    using Papercut.Core.Settings;
    using Papercut.Service.Helpers;
    using Module = Autofac.Module;

    public class PapercutServiceModule : Module, IPluginModule
    {
        public string Name => "Papercut Service";
        public string Version => Assembly.GetExecutingAssembly().GetVersion();
        public string Description => "Papercut's Backend Service";
        public IEnumerable<IModule> Modules => new[] {this};

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .Where(type => type.Namespace != null && type.Namespace.EndsWith("Services"))
                .AsImplementedInterfaces()
                .AsSelf()
                .SingleInstance();

            builder.Register(
                ctx => ctx.Resolve<ISettingStore>().UseTyped<PapercutServiceSettings>())
                .AsSelf()
                .SingleInstance();

            builder.Register((c) => new ApplicationMeta("Papercut.Service"))
                .As<IAppMeta>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}