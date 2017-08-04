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
    using System.Reflection;

    using Autofac;
    using Autofac.Core;

    using Papercut.Core.Domain.Network;
    using Papercut.Core.Domain.Network.Smtp;
    using Papercut.Core.Infrastructure.Plugins;
    using Papercut.Network.Protocols;
    using Papercut.Network.Smtp;

    using Module = Autofac.Module;

    public class PapercutNetworkModule : Module, IDiscoverableModule
    {
        public IModule Module => this;

        protected override void Load(ContainerBuilder builder)
        {
            // server/connections
            builder.RegisterType<SmtpProtocol>()
                .Keyed<IProtocol>(ServerProtocolType.Smtp)
                .InstancePerDependency();

            builder.RegisterType<PapercutProtocol>()
                .Keyed<IProtocol>(ServerProtocolType.Papercut)
                .InstancePerDependency();

            builder.RegisterType<PapercutClient>().AsSelf().InstancePerDependency();
            //builder.RegisterType<SmtpClient>().AsSelf().InstancePerDependency();

            // register smtp commands
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .AssignableTo<ISmtpCommand>()
                .As<ISmtpCommand>()
                .InstancePerDependency();

            builder.RegisterType<ConnectionManager>().AsSelf().InstancePerDependency();
            builder.RegisterType<Connection>().AsSelf().InstancePerDependency();
            builder.RegisterType<Server>().As<IServer>().InstancePerDependency();
        }
    }
}