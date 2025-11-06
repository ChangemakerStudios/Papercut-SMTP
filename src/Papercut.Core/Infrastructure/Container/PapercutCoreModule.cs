// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
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

using Papercut.Common.Domain;
using Papercut.Core.Domain.BackgroundTasks;
using Papercut.Core.Domain.Paths;
using Papercut.Core.Domain.Settings;
using Papercut.Core.Infrastructure.BackgroundTasks;
using Papercut.Core.Infrastructure.Logging;
using Papercut.Core.Infrastructure.MessageBus;

namespace Papercut.Core.Infrastructure.Container;

using Module = Autofac.Module;

public class PapercutCoreModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        RegisterLogging.Register(builder);

        builder.RegisterType<BackgroundTaskRunner>().As<IBackgroundTaskRunner>().SingleInstance();

        //builder.RegisterType<AutofacServiceProvider>()
        //    .As<IServiceProvider>()
        //    .InstancePerLifetimeScope();

        // events
        builder.RegisterType<AutofacMessageBus>()
            .As<IMessageBus>()
            .AsSelf()
            .InstancePerLifetimeScope()
            .PreserveExistingDefaults();

        builder.RegisterType<MessagePathConfigurator>()
            .AsSelf()
            .SingleInstance();

        builder.RegisterType<LoggingPathConfigurator>()
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

                        // immediately save all settings
                        j.Instance.Save();
                    }
                    catch
                    {
                    }
                })
            .AsSelf()
            .SingleInstance();
    }
}