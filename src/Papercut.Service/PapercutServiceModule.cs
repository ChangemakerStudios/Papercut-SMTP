// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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


using System.Reflection;

using Autofac;

using AutofacSerilogIntegration;

using Papercut.Common.Domain;
using Papercut.Common.Helper;
using Papercut.Core.Domain.Application;
using Papercut.Core.Domain.Message;
using Papercut.Core.Domain.Paths;
using Papercut.Service.Infrastructure;
using Papercut.Service.Infrastructure.Paths;
using Papercut.Service.Infrastructure.SmtpServer;
using Papercut.Service.Web.Notification;

using SmtpServer.Storage;

using Module = Autofac.Module;

namespace Papercut.Service;

public class PapercutServiceModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<NewMessageEventHandler>().As<IEventHandler<NewMessageEvent>>().InstancePerLifetimeScope();
        builder.RegisterType<MessagePathConfigurator>().As<IMessagePathConfigurator>().InstancePerLifetimeScope();
        builder.RegisterType<ServerPathTemplateProviderService>().As<IPathTemplatesProvider>().InstancePerLifetimeScope();

        builder.RegisterType<SmtpMessageStore>().As<IMessageStore>().AsSelf();
        builder.RegisterType<SimpleMediatorBus>().As<IMessageBus>();

        builder.Register<IAppMeta>(_ => new ApplicationMeta("Papercut.Service", Assembly.GetExecutingAssembly().GetVersion()));

        builder.RegisterLogger();
    }
}