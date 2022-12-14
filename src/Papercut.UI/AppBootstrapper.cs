/*  
 * Papercut
 *
 *  Copyright © 2008 - 2012 Ken Robertson
 *  Copyright © 2013 - 2020 Jaben Cargman
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
    using System;
    using System.Collections.Generic;
    using System.Management.Instrumentation;
    using System.Windows;

    using Autofac;

    using Caliburn.Micro;

    using Papercut.Core.Annotations;
    using Papercut.ViewModels;
    using Serilog;

    using Papercut.Infrastructure.LifecycleHooks;
    using Papercut.Domain.LifecycleHooks;

    public class AppBootstrapper : BootstrapperBase
    {
        readonly Lazy<ILifetimeScope> _lifetimeScope = new Lazy<ILifetimeScope>(() => ((App)Application.Current).Container);

        public AppBootstrapper()
        {
            Initialize();
        }

        protected ILifetimeScope Container => _lifetimeScope.Value;

        protected override async void OnStartup(object sender, StartupEventArgs e)
        {
            try
            {
                // run prestart
                if (await this.Container.RunPreStart() == AppLifecycleActionResultType.Cancel)
                {
                    Application.Shutdown();
                    return;
                }

                base.OnStartup(sender, e);

                await DisplayRootViewForAsync(typeof(MainViewModel));

                await this.Container.RunStarted();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Fatal Error Starting Papercut");
            }
        }

        protected override void Configure()
        {
            MessageBinder.SpecialValues.Add(
                "$originalsourcecontext",
                context =>
                {
                    var args = context.EventArgs as RoutedEventArgs;
                    var fe = args?.OriginalSource as FrameworkElement;

                    return fe?.DataContext;
                });

            base.Configure();
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return
                Container.Resolve(
                    typeof(IEnumerable<>)
                    .MakeGenericType(service)) as IEnumerable<object>;
        }

        protected override object GetInstance([NotNull] Type service, string named)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            if (string.IsNullOrWhiteSpace(named))
            {
                if (Container.TryResolve(service, out var result)) return result;
            }
            else
            {
                if (Container.TryResolveNamed(named, service, out var result)) return result;
            }

            throw new InstanceNotFoundException($"Could not locate any instances of contract {named ?? service.Name}.");
        }

        protected override void BuildUp(object instance)
        {
            Container.InjectProperties(instance);
        }
    }
}