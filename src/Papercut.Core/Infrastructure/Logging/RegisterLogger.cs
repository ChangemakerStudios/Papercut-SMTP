// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2021 Jaben Cargman
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


namespace Papercut.Core.Infrastructure.Logging
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using Autofac;

    using AutofacSerilogIntegration;

    using Papercut.Common.Domain;
    using Papercut.Core.Domain.Application;
    using Papercut.Core.Domain.Paths;
    using Serilog;
    using Serilog.Debugging;

    /// <summary>
    /// Logging module is pulled into Core
    /// </summary>
    internal sealed class RegisterLogging
    {
        internal static void Register(ContainerBuilder builder)
        {
            builder.Register(c =>
                    {
                        var appMeta = c.Resolve<IAppMeta>();
                        var loggingPathConfigurator = c.Resolve<ILoggingPathConfigurator>();

                        string logFilePath = Path.Combine(
                            loggingPathConfigurator.DefaultSavePath,
                            $"{appMeta.AppName}.log");

                        // support self-logging
                        SelfLog.Enable(s => Console.Error.WriteLine(s));

                        LoggerConfiguration logConfiguration =
                            new LoggerConfiguration()
#if DEBUG
                                .MinimumLevel.Verbose()
#else
                                .MinimumLevel.Information()
#endif
                                .Enrich.With<EnvironmentEnricher>()
                                .Enrich.FromLogContext()
                                .Enrich.WithProperty("AppName", appMeta.AppName)
                                .Enrich.WithProperty("AppVersion", appMeta.AppVersion)
                                .WriteTo.Console()
                                .WriteTo.File(logFilePath);

                        foreach (var configureInstance in c.Resolve<IEnumerable<IConfigureLogging>>().ToList())
                        {
                            configureInstance.Configure(logConfiguration);
                        }

                        return logConfiguration;
                    })
                .AsSelf()
                .SingleInstance();

            builder.Register(
                    c =>
                    {
                        Log.Logger = c.Resolve<LoggerConfiguration>().CreateLogger();
                        return Log.Logger;
                    })
                .AutoActivate()
                .SingleInstance();

            builder.RegisterLogger();
        }
    }
}