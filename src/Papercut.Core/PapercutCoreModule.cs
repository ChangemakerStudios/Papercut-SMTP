// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2016 Jaben Cargman
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

namespace Papercut.Core
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Autofac;
    using Autofac.Core;
    using Papercut.Core.Configuration;
    using Papercut.Core.Events;
    using Papercut.Core.Helper;
    using Papercut.Core.Network;
    using Papercut.Core.Plugins;
    using Papercut.Core.Settings;
    using Serilog;
    using Module = Autofac.Module;

    internal class PapercutCoreModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            RegisterPluginArchitecture(builder);

            //builder.RegisterAssemblyModules(PapercutContainer.ExtensionAssemblies);

            builder.Register(c => PluginStore.Instance).As<IPluginStore>().SingleInstance();
            builder.RegisterType<PluginReport>().AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<AutofacServiceProvider>()
                .As<IServiceProvider>()
                .InstancePerLifetimeScope();

            // events
            builder.RegisterType<AutofacMessageBus>()
                .As<IMessageBus>()
                .AsSelf()
                .InstancePerLifetimeScope()
                .PreserveExistingDefaults();

            builder.RegisterType<MessagePathConfigurator>()
                .As<IMessagePathConfigurator>()
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
                        }
                        catch
                        {
                        }
                    })
                .OnRelease(
                    j =>
                    {
                        try
                        {
                            j.Save();
                        }
                        catch
                        {
                        }
                    })
                .AsSelf()
                .SingleInstance();

            RegisterLogger(builder);
        }

        private void RegisterPluginArchitecture(ContainerBuilder builder)
        {
            var scannableAssemblies = PapercutContainer.ExtensionAssemblies;

            var pluginModules =
                scannableAssemblies.SelectMany(a => a.GetExportedTypes())
                    .Where(s =>
                    {
                        var interfaces = s.GetInterfaces();
                        return interfaces.Contains(typeof(IDiscoverableModule)) || interfaces.Contains(typeof(IPluginModule));
                    })
                    .Distinct()
                    .ToList();

            foreach (var pluginType in pluginModules)
            {
                try
                {
                    // register and load...
                    var module = Activator.CreateInstance(pluginType) as IDiscoverableModule;
                    
                    if (module != null)
                    {
                        builder.RegisterModule(module.Module);
                    }

                    var plugin = module as IPluginModule;
                    if (plugin != null)
                    {
                        PluginStore.Instance.Add(plugin);
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Failure Loading Plugin Module Type {PluginModuleType}", pluginType.FullName);
                }
            }
        }

        private static void RegisterLogger(ContainerBuilder builder)
        {
            builder.Register(c =>
            {
                var appMeta = c.Resolve<IAppMeta>();

                string logFilePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Logs",
                    $"{appMeta.AppName}.log");

                LoggerConfiguration logConfiguration =
                    new LoggerConfiguration()
#if DEBUG
                        .MinimumLevel.Verbose()
#else
                         .MinimumLevel.Information()
#endif
                        .Enrich.With<EnvironmentEnricher>()
                        .Enrich.WithThreadId()
                        .Enrich.FromLogContext()
                        .Enrich.WithProperty("AppName", appMeta.AppName)
                        .Enrich.WithProperty("AppVersion", appMeta.AppVersion)
                        .WriteTo.ColoredConsole()
                        .WriteTo.RollingFile(logFilePath);

                // publish event so additional sinks, enrichers, etc can be added before logger creation is finalized.
                try
                {
                    c.Resolve<IMessageBus>().Publish(new ConfigureLoggerEvent(logConfiguration));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Failure Publishing ConfigurationLoggerEvent: " + ex.ToString());
                }

                // support self-logging
                Serilog.Debugging.SelfLog.Out = Console.Error;

                return logConfiguration;
            }).AsSelf().SingleInstance();

            builder.Register(
                c =>
                {
                    Log.Logger = c.Resolve<LoggerConfiguration>().CreateLogger();

                    return Log.Logger;
                }).As<ILogger>().SingleInstance();
        }

        protected override void AttachToComponentRegistration(
            IComponentRegistry componentRegistry,
            IComponentRegistration registration)
        {
            // Handle constructor parameters.
            registration.Preparing += OnComponentPreparing;
        }

        void OnComponentPreparing(object sender, PreparingEventArgs e)
        {
            Type t = e.Component.Activator.LimitType;
            e.Parameters =
                e.Parameters.Union(
                    new[]
                    {
                        new ResolvedParameter(
                            (p, i) => p.ParameterType == typeof(ILogger),
                            (p, i) => i.Resolve<ILogger>().ForContext(t))
                    });
        }
    }
}