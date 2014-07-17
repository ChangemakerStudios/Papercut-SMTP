// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2014 Jaben Cargman
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
    using System;
    using System.IO;
    using System.Reflection;

    using Autofac;
    using Autofac.Core;

    using Papercut.Core.Configuration;
    using Papercut.Core.Settings;
    using Papercut.Service.Classes;

    using Module = Autofac.Module;

    public class PapercutServiceModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // only used if primary papercut is not loaded.
            builder.RegisterType<ServerPathTemplateProvider>()
                .As<IPathTemplatesProvider>()
                .AsSelf()
                .PreserveExistingDefaults();

            builder.RegisterType<ReplyWithDefaultMessageSavePath>()
                .AsImplementedInterfaces()
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<UpdateRulesService>()
                .AsImplementedInterfaces()
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<ApplicationJsonSettingStore>()
                .As<ISettingStore>()
                .AsSelf()
                .SingleInstance();

            builder.Register(
                ctx => ctx.Resolve<ISettingStore>().UseTyped<PapercutServiceSettings>())
                .AsSelf()
                .SingleInstance();

            builder.Register((c) => new ApplicationMeta("Papercut.Service"))
                .As<IAppMeta>()
                .SingleInstance();

            builder.RegisterType<PapercutService>()
                .AsImplementedInterfaces()
                .AsSelf()
                .SingleInstance();
        }
    }
}