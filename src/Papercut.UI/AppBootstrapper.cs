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


using System.Windows;
using System.Windows.Threading;

using Autofac;

using Caliburn.Micro;

using Papercut.Domain.LifecycleHooks;
using Papercut.Infrastructure.LifecycleHooks;
using Papercut.ViewModels;

namespace Papercut
{
    public class AppBootstrapper : BootstrapperBase
    {
        public AppBootstrapper()
        {
            this.Initialize();
        }

        protected ILifetimeScope Container => ((App)Application.Current).Container;

        protected override void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            Container.Resolve<ILogger>().Error(e.Exception, "Caught Unhandled Exception");

            MessageBox.Show($"Error: {e.Exception?.Message}", "Unhandled Exception");
        }

        protected override async void OnStartup(object sender, StartupEventArgs e)
        {
            try
            {
                // run prestart
                if (await this.Container.RunPreStart() == AppLifecycleActionResultType.Cancel)
                {
                    this.Application.Shutdown();
                    return;
                }

                base.OnStartup(sender, e);

                await this.DisplayRootViewForAsync(typeof(MainViewModel));

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
            return this.Container.Resolve(
                    typeof(IEnumerable<>)
                    .MakeGenericType(service)) as IEnumerable<object> ?? [];
        }

        protected override object GetInstance(Type service, string? named)
        {
            ArgumentNullException.ThrowIfNull(service);

            if (string.IsNullOrWhiteSpace(named))
            {
                if (this.Container.TryResolve(service, out var result)) return result;
            }
            else
            {
                if (this.Container.TryResolveNamed(named, service, out var result)) return result;
            }

            throw new Exception($"Could not locate any instances of contract {named ?? service.Name}.");
        }

        protected override void BuildUp(object instance)
        {
            this.Container.InjectProperties(instance);
        }
    }
}