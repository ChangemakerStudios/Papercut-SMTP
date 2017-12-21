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

namespace Papercut.Network
{

    using Autofac;
    using Autofac.Core;
    using Papercut.Core.Infrastructure.Plugins;
    using Papercut.DesktopService.Events;

    using Module = Autofac.Module;
    using Papercut.Common.Domain;
    using Papercut.Core.Domain.Message;
    using Papercut.Core.Infrastructure.Lifecycle;
    using Papercut.Service.Web.Hosting;

    public class PapercutDesktopServiceModule : Module, IDiscoverableModule
    {
        public IModule Module => this;

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<NewMessageReceivedEvent>()
                .AsSelf()
                .As<IEventHandler<NewMessageEvent>>()
                .SingleInstance();

            builder.RegisterType<ServiceReadyEvent>()
                .AsSelf()
                .As<IEventHandler<PapercutServiceReadyEvent>>()
                .SingleInstance();

            builder.RegisterType<WebServerReadyEvent>()
                .AsSelf()
                .As<IEventHandler<PapercutWebServerReadyEvent>>()
                .SingleInstance();
        }
    }
}