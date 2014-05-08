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

namespace Papercut.Core
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;

    using Autofac;

    public static class PapercutContainer
    {
        static readonly SafeReadWriteProvider<IContainer> _containerProvider
            = new SafeReadWriteProvider<IContainer>(Build);

        public static readonly object UIScopeTag = new object();

        static readonly Lazy<Assembly[]> _extensionAssemblies = new Lazy<Assembly[]>(
            () => new AssemblyScanner()
                      .GetAll()
                      .Except(Assembly.GetExecutingAssembly().ToEnumerable())
                      .Where(s => s.FullName.StartsWith("Papercut"))
                      .Distinct()
                      .ToArray());

        static PapercutContainer()
        {
            AppDomain.CurrentDomain.ProcessExit += DisposeContainer;
        }

        public static Assembly[] ExtensionAssemblies
        {
            get
            {
                return _extensionAssemblies.Value;
            }
        }

        public static IContainer Instance
        {
            get
            {
                return _containerProvider.Instance;
            }
        }

        static void DisposeContainer(object sender, EventArgs e)
        {
            try
            {
                if (_containerProvider.Created)
                {
                    _containerProvider.Instance.Dispose();
                    _containerProvider.Instance = null;
                }
            }
            catch (ObjectDisposedException)
            {
            }
        }

        static IContainer Build()
        {
            var builder = new ContainerBuilder();

            builder.RegisterModule<PapercutCoreModule>();

            return builder.Build();
        }
    }
}