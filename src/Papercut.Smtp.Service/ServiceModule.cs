// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2022 Jaben Cargman
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

using Papercut.Core.Infrastructure.Container;

namespace Papercut.Smtp.Service
{
    [PublicAPI]
    public class ServiceModule : Autofac.Module
    {
        private IEnumerable<IModule> GetPapercutServiceModules()
        {
            yield return new PapercutCoreModule();
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

            builder.Register(c => new ApplicationMeta("Papercut.Service"))
                .As<IAppMeta>()
                .SingleInstance();

            builder.RegisterStaticMethods(this.ThisAssembly);

            base.Load(builder);
        }
    }
}