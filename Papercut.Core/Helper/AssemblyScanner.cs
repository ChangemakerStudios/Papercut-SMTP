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
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    public class AssemblyScanner
    {
        [NotNull]
        public IEnumerable<Assembly> GetAll()
        {
            var filterAssemblies = new Func<Assembly, bool>(a => !a.IsDynamic && !a.GlobalAssemblyCache);

            // get all currently loaded assemblies sans GAC and Dynamic assemblies.
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(filterAssemblies).ToList();
            var loadedFiles = loadedAssemblies.Select(a => Path.GetFileName(a.CodeBase)).ToList();

            // get all files...
            var allFiles = GetAllFilesIn(AppDomain.CurrentDomain.BaseDirectory).ToList();

            // exclude currently loaded assemblies
            var needsToBeLoaded = allFiles
                .Where(f => !loadedFiles.Contains(Path.GetFileName(f), StringComparer.OrdinalIgnoreCase))
                .ToList();

            // attempt to load files as an assembly and include already loaded
            var aggregatedAssemblies = TryLoadAssemblies(needsToBeLoaded)
                .Where(filterAssemblies)
                .Concat(loadedAssemblies)
                .ToList();

            // load resource assemblies
            var loadedAssemblyNames = loadedAssemblies.Select(a => a.GetName().Name).ToArray();
            var assemblyResourcesToLoad =
                GetAllAssemblyResourcesIn(loadedAssemblies)
                    .Where(s => !loadedAssemblyNames.Contains(s, StringComparer.OrdinalIgnoreCase))
                    .ToArray();

            return
                TryLoadResourceAssemblies(assemblyResourcesToLoad)
                    .Where(filterAssemblies)
                    .Concat(aggregatedAssemblies)
                    .ToList();
        }

        static IEnumerable<Assembly> TryLoadAssemblies([NotNull] IEnumerable<string> filenames)
        {
            foreach (var assemblyFile in filenames.Where(File.Exists))
            {
                Assembly assembly;

                try
                {
                    assembly = Assembly.LoadFrom(assemblyFile);
                }
                catch (BadImageFormatException)
                {
                    // fail on native images...
                    continue;
                }

                yield return assembly;
            }
        }

        static IEnumerable<Assembly> TryLoadResourceAssemblies([NotNull] IEnumerable<string> assemblyNames)
        {
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

                yield return assembly;
            }
        }

        [NotNull]
        static IEnumerable<string> GetAllFilesIn(string directory)
        {
            var lookFor = new[] { "*.dll", "*.exe" };

            return lookFor.SelectMany(s => Directory.GetFiles(directory, s)).ToArray();
        }

        static IEnumerable<string> GetAllAssemblyResourcesIn(IEnumerable<Assembly> assemblies)
        {
            var lookFor = new [] { ".dll", ".exe" };

            return
                assemblies.SelectMany(a => a.GetManifestResourceNames())
                    .Where(r => lookFor.Any(r.EndsWith))
                    .Select(Path.GetFileNameWithoutExtension)
                    .ToArray();
        }
    }
}