// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2014 Jaben Cargman
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
    using Papercut.Core.Message;
    using Papercut.Core.Network;
    using Papercut.Core.Rules;
    using Papercut.Core.Settings;

    using Serilog;

    using Module = Autofac.Module;

    class PapercutCoreModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            Assembly[] scannableAssemblies = PapercutContainer.ExtensionAssemblies;

            builder.RegisterAssemblyModules<IModule>(scannableAssemblies);

            // server/connections
            builder.RegisterType<SmtpProtocol>()
                .Keyed<IProtocol>(ServerProtocolType.Smtp)
                .InstancePerDependency();

            builder.RegisterType<PapercutProtocol>()
                .Keyed<IProtocol>(ServerProtocolType.Papercut)
                .InstancePerDependency();

            builder.RegisterType<PapercutClient>().AsSelf().InstancePerDependency();
            //builder.RegisterType<SmtpClient>().AsSelf().InstancePerDependency();

            builder.RegisterType<ConnectionManager>().AsSelf().InstancePerDependency();
            builder.RegisterType<Connection>().AsSelf().InstancePerDependency();
            builder.RegisterType<Server>().As<IServer>().InstancePerDependency();

            builder.RegisterType<AutofacServiceProvider>()
                .As<IServiceProvider>()
                .InstancePerLifetimeScope();

            // events
            builder.RegisterType<AutofacPublishEvent>()
                .As<IPublishEvent>()
                .AsSelf()
                .InstancePerLifetimeScope()
                .PreserveExistingDefaults();

            // rules and rule dispatchers
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .AssignableTo<IRule>()
                .As<IRule>()
                .InstancePerDependency();

            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .AsClosedTypesOf(typeof(IRuleDispatcher<>))
                .AsImplementedInterfaces()
                .AsSelf()
                .InstancePerDependency();

            builder.RegisterType<RulesRunner>().As<IRulesRunner>().SingleInstance();

            builder.RegisterType<MessageRepository>().AsSelf().SingleInstance();
            builder.RegisterType<RuleRespository>().AsSelf().SingleInstance();
            builder.RegisterType<MimeMessageLoader>().AsSelf().SingleInstance();

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

            builder.Register(
                c =>
                {
                    var appMeta = c.Resolve<IAppMeta>();

                    string logFilePath = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        string.Format("{0}.log", appMeta.AppName));

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
                        c.Resolve<IPublishEvent>().Publish(new ConfigureLoggerEvent(logConfiguration));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Failure Publishing ConfigurationLoggerEvent: " + ex.ToString());
                    }

                    Log.Logger = logConfiguration.CreateLogger();

                    // support self-logging
                    Serilog.Debugging.SelfLog.Out = Console.Error;

                    return Log.Logger;
                }).SingleInstance();

            base.Load(builder);
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