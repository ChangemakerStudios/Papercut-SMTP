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
    using Microsoft.Extensions.DependencyModel.Resolution;
    using System.Text;

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
            AssemblyLoadContext.Default.Resolving += AssemblyResolver.OnResolving;

            foreach (string assemblyFile in filenames.Where(File.Exists))
            {
                Assembly assembly;

                try
                {
                    var assemblyName = AssemblyLoadContext.GetAssemblyName(assemblyFile);
                    if (IsInRuntimeLibraries(assemblyName))
                    {
                        assembly = Assembly.Load(assemblyName);
                    }
                    else
                    {
                        assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyFile);
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


        static bool IsInRuntimeLibraries(AssemblyName assemblyName)
        {
            return DependencyContext.Default.CompileLibraries.Any(library => string.Equals(library.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase))
                 || DependencyContext.Default.RuntimeLibraries.Any(library => string.Equals(library.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));
        }

        [NotNull]
        IEnumerable<string> GetAllFilesIn(string directory)
        {
            var lookFor = new[] { "*.dll" };

            return lookFor.SelectMany(s => Directory.GetFiles(directory, s)).ToArray();
        }


        static class AssemblyResolver
        {
            static ICompilationAssemblyResolver assemblyResolver;
            static List<RuntimeLibrary> allLibraries;

            static AssemblyResolver() {
                allLibraries = Directory.GetFiles(PlatformServices.Default.Application.ApplicationBasePath, "*.deps.json")
                        .SelectMany(deps =>
                        {
                            var reader = new DependencyContextJsonReader();
                            using (var json = File.OpenRead(deps))
                            {
                                return reader.Read(json).RuntimeLibraries;
                            }
                        })
                        .Distinct(new RuntimeLibraryComparer())
                        .ToList();

                assemblyResolver = new CompositeCompilationAssemblyResolver
                                        (new ICompilationAssemblyResolver[]
                {
                    new AppBaseCompilationAssemblyResolver(Path.GetDirectoryName(PlatformServices.Default.Application.ApplicationBasePath)),
                    new ReferenceAssemblyPathResolver(),
                    new PackageCompilationAssemblyResolver(FindPackageRoot())
                });
            }

           
            public static Assembly OnResolving(AssemblyLoadContext context, AssemblyName dependency)
            {
                bool NamesMatch(RuntimeLibrary runtime)
                {
                    return string.Equals(runtime.Name, dependency.Name, StringComparison.OrdinalIgnoreCase);
                }

                // avoid loading *.resources dlls, because of: https://github.com/dotnet/coreclr/issues/8416
                if (dependency.Name.EndsWith("resources"))
                {
                    return null;
                }

                RuntimeLibrary library = allLibraries.FirstOrDefault(NamesMatch);
                if (library != null)
                {
                    var wrapper = new CompilationLibrary(
                        library.Type,
                        library.Name,
                        library.Version,
                        library.Hash,
                        library.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths),
                        library.Dependencies,
                        library.Serviceable);

                    var assemblies = new List<string>();
                    assemblyResolver.TryResolveAssemblyPaths(wrapper, assemblies);
                    if (assemblies.Count > 0)
                    {
                        return AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblies[0]);
                    }
                }

                return null;
            }

            static bool IsCandidateLibrary(RuntimeLibrary library, AssemblyName assemblyName)
            {
                return string.Equals(library.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase)
                    || (library.Dependencies.Any(d => d.Name.StartsWith(assemblyName.Name)));
            }

            static string FindPackageRoot()
            {
                var packageLib = DependencyContext.Default.CompileLibraries
                        .Where(lib => lib.Type == "package")
                        .Take(2)
                        .Select(lib => Assembly.Load(new AssemblyName(lib.Name)).Location)
                        .ToList();

                var firstPath = packageLib[0].ToLowerInvariant().Split(Path.DirectorySeparatorChar);
                var secondPath = packageLib[1].ToLowerInvariant().Split(Path.DirectorySeparatorChar);
                var min = Math.Min(firstPath.Length, secondPath.Length);
                var diffIndex = -1;
                for (var i = 0; i < min; i++)
                {
                    if (firstPath[i] != secondPath[i])
                    {
                        diffIndex = i;
                        break;
                    }
                }

                if (diffIndex > -1)
                {
                    return string.Join(Path.DirectorySeparatorChar.ToString(), firstPath.Take(diffIndex));
                }

                return null;
            }


            class RuntimeLibraryComparer : IEqualityComparer<RuntimeLibrary>
            {
                public bool Equals(RuntimeLibrary x, RuntimeLibrary y)
                {
                    if (x == null && y == null)
                    {
                        return true;
                    }

                    return x?.Name == y?.Name; //  && x?.Version == y?.Version;
                }

                public int GetHashCode(RuntimeLibrary obj)
                {
                    return obj.Name.GetHashCode() + 10000;
                    // return string.Concat(obj.Name, '/', obj.Version).GetHashCode();
                }
            }

        }
    }
}