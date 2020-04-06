// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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

namespace Papercut.Smtp.Service
{
    using System.Collections.Generic;
    using System.Reflection;

    using Autofac;

    using Common.Domain;

    using Core.Annotations;
    using Core.Domain.Application;
    using Core.Domain.Paths;
    using Core.Domain.Settings;
    using Core.Infrastructure.MessageBus;

    using Helpers;

    using Infrastructure.IPComm;
    using Infrastructure.Smtp;

    using Message;

    using Rules;

    using Module = Autofac.Module;

    [PublicAPI]
    public class ServiceModule : Module
    {
        private IEnumerable<Module> GetPapercutServiceModules()
        {
            yield return new PapercutMessageModule();
            yield return new PapercutIPCommModule();
            yield return new PapercutRuleModule();
            yield return new PapercutSmtpModule();
            //yield return new PapercutWebApiModule();
        }

        protected override void Load(ContainerBuilder builder)
        {
            foreach (var module in this.GetPapercutServiceModules())
            {
                builder.RegisterModule(module);
            }

            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .Where(type => type.Namespace != null && type.Namespace.EndsWith("Services"))
                .AsImplementedInterfaces()
                .AsSelf()
                .InstancePerLifetimeScope();

            builder.Register(
                    ctx => ctx.Resolve<ISettingStore>().UseTyped<PapercutServiceSettings>())
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<CleanupQueue>().AsSelf().InstancePerLifetimeScope();

            // events
            builder.RegisterType<AutofacMessageBus>()
                .As<IMessageBus>()
                .AsSelf()
                .InstancePerLifetimeScope()
                .PreserveExistingDefaults();

            builder.RegisterType<MessagePathConfigurator>()
                .As<IMessagePathConfigurator>()
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<JsonSettingStore>()
                .As<ISettingStore>()
                .OnActivated(
                    j =>
                    {
                        try
                        {
                            j.Instance.Load();
                        }
                        catch
                        {
                        }
                    })
                .OnRelease(
                    j =>
                    {
                        try
                        {
                            j.Save();
                        }
                        catch
                        {
                        }
                    })
                .AsSelf()
                .SingleInstance();

            builder.Register(c => new ApplicationMeta("Papercut.Service"))
                .As<IAppMeta>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}