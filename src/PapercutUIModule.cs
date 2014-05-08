/*  
 * Papercut
 *
 *  Copyright © 2008 - 2012 Ken Robertson
 *  Copyright © 2013 - 2014 Jaben Cargman
 *  
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *  
 *  http://www.apache.org/licenses/LICENSE-2.0
 *  
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *  
 */

namespace Papercut
{
    using Autofac;
    using Autofac.Core;

    using Caliburn.Micro;

    using Papercut.Core;
    using Papercut.Core.Configuration;
    using Papercut.Core.Events;
    using Papercut.Core.Helper;
    using Papercut.Core.Setting;
    using Papercut.Events;
    using Papercut.Helpers;
    using Papercut.Services;
    using Papercut.UI;

    public class PapercutUIModule : Module
    {
        #region Methods

        protected override void Load(ContainerBuilder builder)
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

            builder.RegisterType<WindowManager>().As<IWindowManager>().InstancePerLifetimeScope();
            builder.RegisterType<EventAggregator>().As<IEventAggregator>().InstancePerLifetimeScope();
            builder.RegisterType<EventPublishAll>().As<IPublishEvent>().InstancePerLifetimeScope();

            builder.RegisterType<WireupLogBridge>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SettingPathTemplateProvider>().As<IPathTemplatesProvider>().SingleInstance();

            base.Load(builder);
        }

        static void SubscribeEventAggregator(IActivatedEventArgs<object> e)
        {
            // Automatically calls subscribe on activated Windows, Views and ViewModels
            e.Context.Resolve<IEventAggregator>().Subscribe(e.Instance);
        }

        #endregion
    }
}