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

namespace Papercut.Core.Infrastructure.AssemblyScanning
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Papercut.Common.Extensions;
    using Papercut.Core.Annotations;

    using Serilog;

    public class AssemblyScanner
    {
        readonly Lazy<ILogger> _logger;
        readonly Func<Assembly> _getEntryAssembly;

        public AssemblyScanner(Lazy<ILogger> logger, Func<Assembly> getEntryAssembly = null)
        {
            this._logger = logger;
            this._getEntryAssembly = getEntryAssembly;
        }

        IEnumerable<Assembly> GetAssembliesList(IEnumerable<string> pluginDirectories)
        {
            var filterAssemblies =
                new Func<Assembly, bool>(a => !a.IsDynamic && !a.GlobalAssemblyCache);

            // get all currently loaded assemblies sans GAC and Dynamic assemblies.
            List<Assembly> loadedAssemblies =
                AppDomain.CurrentDomain.GetAssemblies().Where(filterAssemblies).ToList();

            List<string> loadedFiles =
                loadedAssemblies.Select(a => Path.GetFileName(a.CodeBase)).ToList();

            // get all files...
            List<string> allFiles = pluginDirectories.SelectMany(this.GetAllFilesIn).Distinct().ToList();

            // exclude currently loaded assemblies
            List<string> needsToBeLoaded = allFiles
                .Where(f => !loadedFiles.Contains(Path.GetFileName(f), StringComparer.OrdinalIgnoreCase))
                .ToList();

            // attempt to load files as an assembly and include already loaded
            List<Assembly> aggregatedAssemblies = this.TryLoadAssemblies(needsToBeLoaded)
                .Where(filterAssemblies)
                .Concat(loadedAssemblies)
                .ToList();

            // get referenced assemblies...
            var allReferenced = (_getEntryAssembly() ?? Assembly.GetEntryAssembly())?.GetReferencedAssemblies().IfNullEmpty().Distinct().ToList();

            // load resource assemblies
            //string[] loadedAssemblyNames = loadedAssemblies.Select(a => a.GetName().Name).ToArray();
            //string[] assemblyResourcesToLoad =
            //    GetAllAssemblyResourcesIn(loadedAssemblies)
            //        .Where(s => !loadedAssemblyNames.Contains(s, StringComparer.OrdinalIgnoreCase))
            //        .ToArray();

            return this.TryLoadReferenced(allReferenced).Where(filterAssemblies).Concat(aggregatedAssemblies).ToArray();
        }

        private IEnumerable<Assembly> TryLoadReferenced(List<AssemblyName> allReferenced)
        {
            var currentlyLoaded = AppDomain.CurrentDomain.GetAssemblies().Select(s => s.GetName().FullName).ToList();

            var excludedStartsWith = new[] { "mscorlib", "System." };

            var assemblyNames = allReferenced
                .Where(s => !currentlyLoaded.Contains(s.FullName) &&
                            !excludedStartsWith.Any(_ => s.FullName.StartsWith(_)))
                .ToList();

            foreach (var assemblyName in assemblyNames)
            {
                Assembly assembly;

                try
                {
                    assembly = Assembly.Load(assemblyName);
                }
                catch (BadImageFormatException)
                {
                    // fail on native images...
                    continue;
                }
                catch (FileLoadException ex)
                {
                    this._logger.Value.Warning(ex, "Failure Loading Assembly Named {@AssemblyName}", assemblyName);
                    continue;
                }

                yield return assembly;

                // attempt to load additional dependencies
                foreach (var a in this.TryLoadReferenced(assembly.GetReferencedAssemblies().IfNullEmpty().ToList()))
                    yield return a;
            }
        }

        [NotNull]
        public IEnumerable<Assembly> GetAll()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            var directories = new string[]
            {
                baseDirectory,
                Path.Combine(baseDirectory, @"modules"),
                Path.Combine(baseDirectory, @"plugins")
            };

            return this.GetAssembliesList(directories.Where(Directory.Exists));
        }

        IEnumerable<Assembly> TryLoadAssemblies([NotNull] IEnumerable<string> filenames)
        {
            foreach (string assemblyFile in filenames.Where(File.Exists))
            {
                Assembly assembly;

                try
                {
                    assembly = Assembly.UnsafeLoadFrom(assemblyFile);
                    var assemblyLoader = assembly.GetType("Costura.AssemblyLoader");
                    if (assemblyLoader != null)
                    {
                        var attach = assemblyLoader.GetMethod("Attach");
                        attach?.Invoke(null, null);
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

        IEnumerable<Assembly> TryLoadResourceAssemblies(
            [NotNull] IEnumerable<string> assemblyNames)
        {
            foreach (string assemblyName in assemblyNames)
            {
                Assembly assembly;

                try
                {
                    assembly = Assembly.Load(assemblyName);
                }
                catch (BadImageFormatException)
                {
                    // fail on native images...
                    continue;
                }
                catch (FileLoadException ex)
                {
                    this._logger.Value.Warning(ex, "Failure Loading Assembly Resource {AssemblyName}", assemblyName);
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

        IEnumerable<string> GetAllAssemblyResourcesIn(IEnumerable<Assembly> assemblies)
        {
            var lookFor = new[] { ".dll", ".exe" };

            return
                assemblies.SelectMany(a => a.GetManifestResourceNames())
                    .Where(r => lookFor.Any(r.EndsWith))
                    .Select(Path.GetFileNameWithoutExtension)
                    .ToArray();
        }
    }
}