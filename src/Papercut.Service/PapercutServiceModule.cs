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

using System;
using Papercut.Common.Domain;
using Papercut.Core.Domain.Message;
using Papercut.Service.Web.Notification;

namespace Papercut.Service
{
    using System.Reflection;
    using Autofac;
    using Autofac.Core;

    using Papercut.Core.Domain.Application;
    using Papercut.Core.Domain.Settings;
    using Papercut.Core.Infrastructure.Plugins;
    using Papercut.Service.Helpers;
    using Module = Autofac.Module;
    using Papercut.Service.Web.Hosting;

    public class PapercutServiceModule : Module, IDiscoverableModule
    {
        public IModule Module => this;

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(this.GetType().GetTypeInfo().Assembly)
                .Where(type => type.Namespace != null && type.Namespace.EndsWith("Services"))
                .AsImplementedInterfaces()
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<WebServerReadyEvent>().As<IEventHandler<PapercutWebServerReadyEvent>>();
            builder.RegisterType<NewMessageEventHandler>().As<IEventHandler<NewMessageEvent>>();

            builder.RegisterType<WebServer>()
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

    public class WebServerReadyEvent: IEventHandler<PapercutWebServerReadyEvent>
    {
        private static Action<PapercutWebServerReadyEvent> _webServerReady;

        public static void Register(Action<PapercutWebServerReadyEvent> webServerReady)
        {
            _webServerReady = webServerReady;
        }
        
        public void Handle(PapercutWebServerReadyEvent @event)
        {
            _webServerReady?.Invoke(@event);
            _webServerReady = null;
        }
    }
}