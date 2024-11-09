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


using Autofac;
using Autofac.Core;

using Caliburn.Micro;

using Microsoft.Extensions.Logging;

using Papercut.Core;
using Papercut.Core.Domain.Application;
using Papercut.Core.Infrastructure.Container;
using Papercut.Helpers;
using Papercut.Infrastructure.IPComm;
using Papercut.Infrastructure.Smtp;
using Papercut.Message;
using Papercut.Rules;

using Velopack;
using Velopack.Sources;

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
            foreach (var module in GetPapercutServiceModules())
            {
                builder.RegisterModule(module);
            }

            RegisterUI(builder);

            // message watcher is needed for watching
            builder.RegisterType<MessageWatcher>().AsSelf().SingleInstance();

            builder.Register(_ => new ApplicationMeta(AppConstants.ApplicationName))
                .As<IAppMeta>()
                .SingleInstance();

            builder.Register(c =>
                new UpdateManager(new GithubSource(AppConstants.UpgradeUrl, null, false),
                    logger: c.ResolveOptional<ILogger<UpdateManager>>())).AsSelf().SingleInstance();

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

            builder.RegisterStaticMethods(ThisAssembly);

            base.Load(builder);
        }

        void RegisterUI(ContainerBuilder builder)
        {
            //  register view models
            builder.RegisterAssemblyTypes(ThisAssembly)
                .Where(type => type.Name.EndsWith("ViewModel"))
                .AsImplementedInterfaces()
                .AsSelf()
                .OnActivated(SubscribeEventAggregator)
                .InstancePerDependency();

            //  register views
            builder.RegisterAssemblyTypes(ThisAssembly)
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