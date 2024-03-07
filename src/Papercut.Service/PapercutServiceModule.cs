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


namespace Papercut.Service
{
    using System.Reflection;

    using Autofac;
    using Autofac.Core;

    using Papercut.Common.Domain;
    using Papercut.Core.Domain.Application;
    using Papercut.Core.Domain.Message;
    using Papercut.Core.Domain.Paths;
    using Papercut.Core.Domain.Settings;
    using Papercut.Core.Infrastructure.Lifecycle;
    using Papercut.Core.Infrastructure.Plugins;
    using Papercut.Service.Helpers;
    using Papercut.Service.Infrastructure.Console;
    using Papercut.Service.Infrastructure.Paths;
    using Papercut.Service.Infrastructure.SmtpServer;
    using Papercut.Service.Infrastructure.WebServer;
    using Papercut.Service.Web.Hosting;
    using Papercut.Service.Web.Notification;

    using SmtpServer.Storage;

    using Module = Autofac.Module;

    public class PapercutServiceModule : Module, IDiscoverableModule
    {
        public IModule Module => this;

        protected override void Load(ContainerBuilder builder)
        {
            RegisterStartupServices(builder);

            builder.RegisterType<NewMessageEventHandler>().As<IEventHandler<NewMessageEvent>>();

            builder.RegisterType<ServerPathTemplateProviderService>().As<IPathTemplatesProvider>().InstancePerLifetimeScope();

            builder.Register(
                    ctx => ctx.Resolve<ISettingStore>().UseTyped<PapercutServiceSettings>())
                .AsSelf()
                .SingleInstance();

            builder.Register(c => new ApplicationMeta("Papercut.Service"))
                .As<IAppMeta>()
                .SingleInstance();

            builder.RegisterType<SmtpMessageStore>().As<MessageStore>().AsSelf();

            //builder.RegisterType<SerilogSmtpServerLoggingBridge>().As<global::SmtpServer.ILogger>();
        }

        private static void RegisterStartupServices(ContainerBuilder builder)
        {
            builder.RegisterType<ConsoleTitleSetterStartupService>().As<IStartupService>().SingleInstance();
            builder.RegisterType<SmtpServerStartup>().As<IStartupService>().SingleInstance();
            builder.RegisterType<WebServer>().As<IStartupService>().SingleInstance();
        }
    }
}