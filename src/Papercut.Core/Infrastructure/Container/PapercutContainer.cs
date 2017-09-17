// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2017 Jaben Cargman
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

namespace Papercut.Core.Infrastructure.Container
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    using Autofac;
    using Papercut.Core.Infrastructure.AssemblyScanning;
    using Papercut.Core.Infrastructure.Logging;

    using Serilog;
    using Serilog.Events;
    using Serilog.Formatting.Json;
    using Serilog.Sinks.RollingFile;
    using Microsoft.Extensions.PlatformAbstractions;
    using System.Runtime.Loader;

    public static class PapercutContainer
    {
        static readonly Lazy<IContainer> _containerProvider;

        static readonly Lazy<ILogger> _rootLogger;

        public static readonly object UIScopeTag = new object();

        public static Assembly SpecifiedEntryAssembly { get; set; }

        static readonly Lazy<Assembly[]> _extensionAssemblies = new Lazy<Assembly[]>(
            () =>
            {
                try
                {
                    return new AssemblyScanner(_rootLogger, () => SpecifiedEntryAssembly)
                            .GetPluginAssemblies()
                            .Distinct()
                            .ToArray();
                }
                catch (Exception ex)
                {
                    _rootLogger.Value.Fatal(ex, "Fatal Failure Loading Extension Assemblies");
                    throw;
                }
            }, LazyThreadSafetyMode.ExecutionAndPublication);

        static PapercutContainer()
        {
            _rootLogger = new Lazy<ILogger>(() =>
            {
                string logFilePath = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath,
                    "Logs",
                    "PapercutCoreFailure.json");

                var jsonSink = new RollingFileSink(logFilePath, new JsonFormatter(), null, null);

                return
                    new LoggerConfiguration().MinimumLevel.Information()
                        .Enrich.With<EnvironmentEnricher>()
                        .WriteTo.LiterateConsole()
                        .WriteTo.Sink(jsonSink, LogEventLevel.Information).CreateLogger();
            });
            _containerProvider = new Lazy<IContainer>(Build, LazyThreadSafetyMode.ExecutionAndPublication);

            AssemblyLoadContext.Default.Unloading += DisposeContainer;
        }
        
        public static Assembly[] ExtensionAssemblies => _extensionAssemblies.Value;

        public static IContainer Instance => _containerProvider.Value;

        static void DisposeContainer(AssemblyLoadContext ctx)
        {
            Debug.WriteLine("ProcessExit Called: Disposing Container");

            try
            {
                if (_containerProvider.IsValueCreated)
                {
                    _containerProvider.Value.Dispose();
                }
            }
            catch (ObjectDisposedException)
            {
            }
        }

        static IContainer Build()
        {
            try
            {
                var builder = new ContainerBuilder();
                builder.RegisterModule<PapercutCoreModule>();
                return builder.Build();
            }
            catch (Exception ex)
            {
                _rootLogger.Value.Fatal(ex, "Fatal Failure Building Container");
                throw;
            }
        }
    }
}