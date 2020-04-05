// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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
    using System.IO;
    using System.Threading.Tasks;

    using Common;

    using Serilog;
    using Serilog.Events;
    using Serilog.Formatting.Json;
    using Serilog.Sinks.RollingFile;

    public static class BootstrapLogger
    {
        static readonly Lazy<ILogger> _rootLogger;

        static BootstrapLogger()
        {
            _rootLogger = new Lazy<ILogger>(() =>
            {
                string logFilePath = Path.Combine(AppConstants.DataDirectory,
                    "Logs",
                    "PapercutCoreFailure.json");

                var jsonSink = new RollingFileSink(logFilePath, new JsonFormatter(), null, null);

                return
                    new LoggerConfiguration().MinimumLevel.Information()
                        .Enrich.With<EnvironmentEnricher>()
                        .WriteTo.LiterateConsole()
                        .WriteTo.Sink(jsonSink, LogEventLevel.Information).CreateLogger();
            });

            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainOnUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
        }

        static void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs args)
        {
            var logInstance = IsLoggerConfigured() ? Log.Logger : Logger;

            if (!args.Observed) logInstance.Error(args.Exception, "Unobserved Task Exception");
        }

        public static void SetRootGlobal()
        {
            Log.Logger = Logger;
        }

        static bool IsLoggerConfigured()
        {
            return !Log.Logger?.GetType().Name.EndsWith("SilentLogger") ?? false;
        }

        static void OnCurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            var logInstance = IsLoggerConfigured() ? Log.Logger : Logger;

            if (args.IsTerminating) logInstance.Fatal(args.ExceptionObject as Exception, "Unhandled Exception");
            else logInstance.Information(args.ExceptionObject as Exception, "Non-Fatal Unhandled Exception");
        }

        public static ILogger Logger => _rootLogger.Value;
    }
}