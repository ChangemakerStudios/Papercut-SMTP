// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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


using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using Microsoft.Extensions.PlatformAbstractions;

using Papercut.Core.Infrastructure.AssemblyScanning;
using Papercut.Core.Infrastructure.Logging;

using Serilog;

namespace Papercut.Core.Infrastructure.Container
{
    public static class PapercutContainer
    {
        #region Static Fields

        public static readonly object UIScopeTag = new object();

        static readonly Lazy<ILogger> _rootLogger;

        static readonly Lazy<Assembly[]> _extensionAssemblies = new Lazy<Assembly[]>(
            () =>
            {
                try
                {
                    return new AssemblyScanner(_rootLogger, () => PapercutCoreModule.SpecifiedEntryAssembly)
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

        #endregion

        #region Constructors and Destructors

        static PapercutContainer()
        {
            _rootLogger = new Lazy<ILogger>(() =>
            {
                string logFilePath = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath,
                    "Logs",
                    "PapercutCoreFailure.json");

                return
                    new LoggerConfiguration().MinimumLevel.Information()
                        .Enrich.With<EnvironmentEnricher>()
                        .WriteTo.Console().CreateLogger();
            });
        }

        #endregion

        #region Public Properties

        public static Assembly[] ExtensionAssemblies => _extensionAssemblies.Value;

        public static ILogger RootLogger => _rootLogger.Value;

        #endregion
    }
}