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
    using System.Windows;

    using Autofac;

    using Caliburn.Micro;

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

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return
                Container.Resolve(
                    typeof(IEnumerable<>)
                    .MakeGenericType(new Type[] { service })) as IEnumerable<object>;
        }

        protected override object GetInstance(Type service, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                object result;
                if (Container.TryResolve(service, out result)) return result;
            }
            else
            {
                object result;
                if (Container.TryResolveNamed(key, service, out result)) return result;
            }

            throw new Exception(string.Format("Could not locate any instances of contract {0}.", key ?? service.Name));
        }

        protected override void BuildUp(object instance)
        {
            Container.InjectProperties(instance);
        }
    }
}