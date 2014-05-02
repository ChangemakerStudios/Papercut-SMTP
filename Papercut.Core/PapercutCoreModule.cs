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
    using System.Linq;
    using System.Reflection;

    using Autofac;
    using Autofac.Core;

    using Papercut.Core.Configuration;
    using Papercut.Core.Message;

    using Serilog;
    using Serilog.Events;

    using Module = Autofac.Module;

    class PapercutCoreModule : Module
    {
        protected Assembly[] GetExtensionAssemblies()
        {
            return new AssemblyScanner()
                .GetAll()
                .Except(Assembly.GetExecutingAssembly().ToEnumerable())
                .Where(s => s.FullName.StartsWith("Papercut"))
                .Distinct()
                .ToArray();
        }

        protected override void Load(ContainerBuilder builder)
        {
            var scannableAssemblies = GetExtensionAssemblies();

            builder.RegisterAssemblyModules<IModule>(scannableAssemblies);

            builder.RegisterType<MessageRepository>().AsSelf().SingleInstance();
            builder.RegisterType<MimeMessageLoader>().AsSelf().SingleInstance();
            builder.RegisterType<MessagePathConfigurator>().As<IMessagePathConfigurator>().AsSelf().SingleInstance();

            builder.Register(
                (c) =>
                {
                    Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Verbose()
                        .Enrich.WithMachineName()
                        .Enrich.WithThreadId()
                        .Enrich.FromLogContext()
                        .WriteTo.ColoredConsole()
                        .WriteTo.RollingFile("papercut.log", LogEventLevel.Debug)
                        //.WriteTo.Seq("http://localhost:5341")
                        .CreateLogger();

                    return Log.Logger;
                }).SingleInstance();

            base.Load(builder);
        }
    }
}