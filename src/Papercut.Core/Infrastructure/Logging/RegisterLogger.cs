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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Autofac;

    using AutofacSerilogIntegration;

    using Papercut.Core.Domain.Application;
    using Papercut.Core.Domain.Paths;
    using Papercut.Core.Infrastructure.CommandLine;

    using Serilog;
    using Serilog.Configuration;
    using Serilog.Debugging;
    using Serilog.Events;

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
                        var loggingPathConfigurator = c.Resolve<LoggingPathConfigurator>();

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
                                .Filter.ByExcluding(ExcludeTcpClientDisposeBugException)
                                .WriteTo.Console()
                                .WriteTo.File(logFilePath)
                                .ReadFrom.KeyValuePairs(ArgumentParser.GetArgsKeyValue(Environment.GetCommandLineArgs().ToArray()));

                        foreach (var configureInstance in c.Resolve<IEnumerable<ILoggerSettings>>().ToList())
                        {
                            logConfiguration.ReadFrom.Settings(configureInstance);
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

        /// <summary>
        /// https://stackoverflow.com/questions/59237011/how-to-avoid-objectdisposed-exception-after-closing-tcpclient
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private static bool ExcludeTcpClientDisposeBugException(LogEvent e)
        {
            var exception = e.Exception?.InnerException;

            if (exception != null)
            {
                var exceptionText = exception.ToString();
                if (exceptionText.Contains("System.Net.Sockets.TcpClient.EndConnect") && exceptionText.Contains("System.NullReferenceException: Object reference not set to an instance of an object"))
                {
                    // exclude this issue -- it's a known bug
                    return true;
                }
            }

            return false;
        }
    }
}