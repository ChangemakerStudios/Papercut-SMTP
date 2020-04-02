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

namespace Papercut.Service
{
    using System.Collections.Generic;
    using System.Reflection;

    using Autofac;

    using Papercut.App.WebApi;
    using Papercut.Core.Annotations;
    using Papercut.Core.Domain.Application;
    using Papercut.Core.Domain.Settings;
    using Papercut.Infrastructure.IPComm;
    using Papercut.Infrastructure.Smtp;
    using Papercut.Message;
    using Papercut.Rules;
    using Papercut.Service.Helpers;
    using Papercut.Service.Logging;

    using Module = Autofac.Module;

    [PublicAPI]
    public class PapercutServiceModule : Module
    {
        private IEnumerable<Module> GetPapercutServiceModules()
        {
            yield return new PapercutMessageModule();
            yield return new PapercutIPCommModule();
            yield return new PapercutRuleModule();
            yield return new PapercutSmtpModule();
            yield return new PapercutWebApiModule();
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

            builder.RegisterType<ConfigureSeqLogging>().AsImplementedInterfaces();

            builder.Register(
                ctx => ctx.Resolve<ISettingStore>().UseTyped<PapercutServiceSettings>())
                .AsSelf()
                .SingleInstance();

            builder.Register(c => new ApplicationMeta("Papercut.Service"))
                .As<IAppMeta>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}