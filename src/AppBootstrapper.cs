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
    using System;
    using System.Collections.Generic;
    using System.Management.Instrumentation;
    using System.Windows;

    using Autofac;

    using Caliburn.Micro;

    using Papercut.Core.Annotations;
    using Papercut.ViewModels;

    public class AppBootstrapper : Bootstrapper<MainViewModel>
    {
        readonly Lazy<ILifetimeScope> _lifetimeScope = new Lazy<ILifetimeScope>(() => ((App)Application.Current).Container);

        protected IComponentContext Container
        {
            get
            {
                return _lifetimeScope.Value;
            }
        }

        protected override void Configure()
        {
            MessageBinder.SpecialValues.Add(
                "$originalsourcecontext",
                context =>
                {
                    var args = context.EventArgs as RoutedEventArgs;
                    if (args == null)
                    {
                        return null;
                    }

                    var fe = args.OriginalSource as FrameworkElement;
                    if (fe == null)
                    {
                        return null;
                    }

                    return fe.DataContext;
                });

            base.Configure();
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return
                Container.Resolve(
                    typeof(IEnumerable<>)
                    .MakeGenericType(new Type[] { service })) as IEnumerable<object>;
        }

        protected override object GetInstance([NotNull] Type service, string named = null)
        {
            if (service == null) throw new ArgumentNullException("service");

            if (string.IsNullOrWhiteSpace(named))
            {
                object result;
                if (Container.TryResolve(service, out result)) return result;
            }
            else
            {
                object result;
                if (Container.TryResolveNamed(named, service, out result)) return result;
            }

            throw new InstanceNotFoundException(string.Format("Could not locate any instances of contract {0}.", named ?? service.Name));
        }

        protected override void BuildUp(object instance)
        {
            Container.InjectProperties(instance);
        }
    }
}