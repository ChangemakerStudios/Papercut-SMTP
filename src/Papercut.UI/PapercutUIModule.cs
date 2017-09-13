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

namespace Papercut
{
    using System.Collections.Generic;
    using System.Reflection;
    using Autofac;
    using Autofac.Core;

    using Caliburn.Micro;

    using Papercut.Common.Domain;
    using Papercut.Core;
    using Papercut.Core.Domain.Application;
    using Papercut.Core.Infrastructure.Container;
    using Papercut.Core.Infrastructure.Plugins;
    using Papercut.Events;
    using Papercut.Helpers;
    using Papercut.Message;

    using Module = Autofac.Module;

    public class PapercutUIModule : Module, IDiscoverableModule
    {
        protected override void Load(ContainerBuilder builder)
        {
            RegisterUI(builder);

            // message watcher is needed for watching
            builder.RegisterType<MessageWatcher>().AsSelf().SingleInstance();

            builder.Register(c => new ApplicationMeta("Papercut"))
                .As<IAppMeta>()
                .SingleInstance();

            // must be single instance
            builder.RegisterType<LogClientSinkQueue>().AsImplementedInterfaces().AsSelf().SingleInstance();

            builder.RegisterType<ViewModelWindowManager>()
                .As<IViewModelWindowManager>()
                .As<IWindowManager>()
                .InstancePerLifetimeScope();

            builder.RegisterType<EventAggregator>()
                .As<IEventAggregator>()
                .InstancePerLifetimeScope();
            builder.RegisterType<EventPublishAll>().As<IMessageBus>().InstancePerLifetimeScope();

            builder.RegisterType<SettingPathTemplateProvider>()
                .AsImplementedInterfaces()
                .SingleInstance();
            builder.RegisterType<WireupLogBridge>().AsImplementedInterfaces().SingleInstance();

            base.Load(builder);
        }

        static void RegisterUI(ContainerBuilder builder)
        {
            //  register view models
            builder.RegisterAssemblyTypes(PapercutContainer.ExtensionAssemblies)
                .Where(type => type.Name.EndsWith("ViewModel"))
                .AsImplementedInterfaces()
                .AsSelf()
                .OnActivated(SubscribeEventAggregator)
                .InstancePerDependency();

            //  register views
            builder.RegisterAssemblyTypes(PapercutContainer.ExtensionAssemblies)
                .Where(type => type.Name.EndsWith("View"))
                .AsImplementedInterfaces()
                .AsSelf()
                .OnActivated(SubscribeEventAggregator)
                .InstancePerDependency();

            // register ui scope services
            builder.RegisterAssemblyTypes(PapercutContainer.ExtensionAssemblies)
                .Where(type => type.Namespace != null && type.Namespace.EndsWith("Services"))
                .AsImplementedInterfaces()
                .AsSelf()
                .InstancePerUIScope();
        }

        static void SubscribeEventAggregator(IActivatedEventArgs<object> e)
        {
            // Automatically calls subscribe on activated Windows, Views and ViewModels
            e.Context.Resolve<IEventAggregator>().Subscribe(e.Instance);
        }

        public IModule Module => this;
    }
}