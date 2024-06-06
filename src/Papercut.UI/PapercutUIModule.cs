﻿// Papercut
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


using Autofac;
using Autofac.Core;

using Caliburn.Micro;

using Papercut.Core;
using Papercut.Core.Domain.Application;
using Papercut.Core.Infrastructure.Container;
using Papercut.Helpers;
using Papercut.Infrastructure.IPComm;
using Papercut.Infrastructure.Smtp;
using Papercut.Message;
using Papercut.Rules;

namespace Papercut
{
    [PublicAPI]
    public class PapercutUIModule : Module
    {
        private IEnumerable<Module> GetPapercutServiceModules()
        {
            yield return new PapercutMessageModule();
            yield return new PapercutIPCommModule();
            yield return new PapercutRuleModule();
            yield return new PapercutSmtpModule();
        }

        protected override void Load(ContainerBuilder builder)
        {
            foreach (var module in this.GetPapercutServiceModules())
            {
                builder.RegisterModule(module);
            }

            this.RegisterUI(builder);

            // message watcher is needed for watching
            builder.RegisterType<MessageWatcher>().AsSelf().SingleInstance();

            builder.Register(_ => new ApplicationMeta(AppConstants.ApplicationName))
                .As<IAppMeta>()
                .SingleInstance();

            builder.RegisterType<ViewModelWindowManager>()
                .As<IViewModelWindowManager>()
                .As<IWindowManager>()
                .InstancePerLifetimeScope();

            builder.RegisterType<EventAggregator>()
                .As<IEventAggregator>()
                .InstancePerLifetimeScope();

            builder.RegisterType<SettingPathTemplateProvider>()
                .AsImplementedInterfaces()
                .SingleInstance();

            builder.RegisterType<WireupLogBridge>().AsImplementedInterfaces().SingleInstance();

            builder.RegisterStaticMethods(this.ThisAssembly);

            base.Load(builder);
        }

        void RegisterUI(ContainerBuilder builder)
        {
            //  register view models
            builder.RegisterAssemblyTypes(this.ThisAssembly)
                .Where(type => type.Name.EndsWith("ViewModel"))
                .AsImplementedInterfaces()
                .AsSelf()
                .OnActivated(SubscribeEventAggregator)
                .InstancePerDependency();

            //  register views
            builder.RegisterAssemblyTypes(this.ThisAssembly)
                .Where(type => type.Name.EndsWith("View"))
                .AsImplementedInterfaces()
                .AsSelf()
                .OnActivated(SubscribeEventAggregator)
                .InstancePerDependency();
        }

        static void SubscribeEventAggregator(IActivatedEventArgs<object> e)
        {
            // Automatically calls subscribe on activated Windows, Views and ViewModels
            e.Context.Resolve<IEventAggregator>().SubscribeOnUIThread(e.Instance);
        }
    }
}