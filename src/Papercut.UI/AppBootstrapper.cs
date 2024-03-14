// Papercut SMTP
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

using Caliburn.Micro;

using Papercut.ViewModels;

namespace Papercut;

public class AppBootstrapper : BootstrapperBase
{
    private readonly Lazy<ILifetimeScope> _lifetimeScope = new(() => ((App)Application.Current).Container);

    public AppBootstrapper()
    {
        this.Initialize();
    }

    protected IComponentContext Container => this._lifetimeScope.Value;

    protected override void OnStartup(object sender, StartupEventArgs e)
    {
        base.OnStartup(sender, e);
        this.DisplayRootViewForAsync<MainViewModel>();
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
                       .MakeGenericType(service)) as IEnumerable<object>;
    }

    protected override object GetInstance(Type service, string? named = null)
    {
        if (service == null) throw new ArgumentNullException(nameof(service));

        if (string.IsNullOrWhiteSpace(named))
        {
            object result;
            if (this.Container.TryResolve(service, out result)) return result;
        }
        else
        {
            object result;
            if (this.Container.TryResolveNamed(named, service, out result)) return result;
        }

        return null;

        //throw new InstanceNotFoundException($"Could not locate any instances of contract {named ?? service.Name}.");
    }

    protected override void BuildUp(object instance)
    {
        this.Container.InjectProperties(instance);
    }
}