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


namespace Papercut.Module.WebUI
{
    using System.Reflection;

    using Autofac;
    using Autofac.Core;
    using Autofac.Integration.WebApi;

    using Core.Infrastructure.Lifecycle;
    using Core.Infrastructure.Plugins;

    using Common.Domain;

    using Module = Autofac.Module;

    public class WebUIPluginModule : Module, IPluginModule
    {
        public IModule Module => this;
        public string Name => "WebUI";
        public string Version => "1.0.0";
        public string Description => "Provides a web UI to manage the email messages for Papercut.";

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<WebServer>()
                .As<IEventHandler<PapercutServiceReadyEvent>>()
                .As<IEventHandler<PapercutClientReadyEvent>>()
                .SingleInstance();

            builder.RegisterApiControllers(ThisAssembly);

            base.Load(builder);
        }
    }
}