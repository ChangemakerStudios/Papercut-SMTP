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

namespace Papercut.Core.Infrastructure.AssemblyScanning
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    
    using Papercut.Core.Annotations;

    using Serilog;
    using Microsoft.Extensions.DependencyModel;
    using System.Runtime.Loader;
    using Microsoft.Extensions.PlatformAbstractions;

    public class AssemblyScanner
    {
        readonly Lazy<ILogger> _logger;
        readonly Func<Assembly> _getEntryAssembly;

        public AssemblyScanner(Lazy<ILogger> logger, Func<Assembly> getEntryAssembly = null)
        {
            this._logger = logger;
            this._getEntryAssembly = getEntryAssembly;
        }


        [NotNull]
        public IEnumerable<Assembly> GetPluginAssemblies()
        {
            var baseDirectory = PlatformServices.Default.Application.ApplicationBasePath;

            var directories = new string[]
            {
                baseDirectory,
                Path.Combine(baseDirectory, @"modules"),
                Path.Combine(baseDirectory, @"plugins")
            };

            return this.GetAssembliesList(directories.Where(Directory.Exists));
        }

        IEnumerable<Assembly> GetAssembliesList(IEnumerable<string> pluginDirectories)
        {
            var filterAssemblies = new Func<Assembly, bool>(a => !a.IsDynamic);
            var loaded = DependencyContext.Default
                                    .RuntimeLibraries
                                    .SelectMany(library => library.Assemblies)
                                    .Select(assembly => assembly.Name.Name)
                                    .Distinct()
                                    .ToList();
            var loadedAssemblies = new HashSet<string>(loaded);

            var thisAssembly = typeof(AssemblyScanner).GetTypeInfo().Assembly.GetName().Name;
            var allFiles = pluginDirectories
                                        .SelectMany(this.GetAllFilesIn)
                                        .Distinct()
                                        .Select(file => new { Path = file, Name = Path.GetFileName(file) })
                                        .Where(assemblyInfo => assemblyInfo.Name.StartsWith("Papercut"))
                                        .ToList();

            var needsToBeLoaded = allFiles
                .Where(f => !loadedAssemblies.Contains(f.Name, StringComparer.OrdinalIgnoreCase))
                .Select(f => f.Path)
                .ToList();

            return this.TryLoadAssemblies(needsToBeLoaded);
        }

        IEnumerable<Assembly> TryLoadAssemblies([NotNull] IEnumerable<string> filenames)
        {
            foreach (string assemblyFile in filenames.Where(File.Exists))
            {
                Assembly assembly;

                try
                {
                    assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyFile);
                    var assemblyLoader = assembly.GetType("Costura.AssemblyLoader");
                    if (assemblyLoader != null)
                    {
                        var attach = assemblyLoader.GetMethod("Attach");
                        attach.Invoke(null, null);
                    }
                }
                catch (BadImageFormatException)
                {
                    // fail on native images...
                    continue;
                }
                catch (FileLoadException ex)
                {
                    this._logger.Value.Warning(ex, "Failure Loading Assembly File {AssemblyFile}", assemblyFile);
                    continue;
                }

                yield return assembly;
            }
        }

        [NotNull]
        IEnumerable<string> GetAllFilesIn(string directory)
        {
            var lookFor = new[] { "*.dll" };

            return lookFor.SelectMany(s => Directory.GetFiles(directory, s)).ToArray();
        }
    }
}