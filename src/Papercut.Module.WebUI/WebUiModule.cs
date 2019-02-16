// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2018 Jaben Cargman
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
    using System;

    using Autofac;
    using Autofac.Core;
    using Autofac.Integration.WebApi;

    using Papercut.Core.Infrastructure.Plugins;

    public class WebUIModule : Module, IDiscoverableModule
    {
        public IModule Module => this;

        public Guid Id => new Guid("F909D3B8-8D35-49C2-BB85-0B3A1DDF4C9B");

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<WebServer>()
                .AsImplementedInterfaces()
                .SingleInstance();

            builder.RegisterApiControllers(this.ThisAssembly);

            base.Load(builder);
        }
    }
}