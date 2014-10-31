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

namespace Papercut.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    public static class AssemblyResolutionHelper
    {
        public static void SetupEmbeddedAssemblyResolve()
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();

            // Code based on: http://www.codingmurmur.com/2014/02/embedded-assembly-loading-with-support.html
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                try
                {
                    string name = args.Name;

                    var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(s => s.GetName().Name == args.Name);

                    if (loadedAssembly != null) return loadedAssembly;

                    var asmName = new AssemblyName(name);

                    // Any retargetable assembly should be resolved directly using normal load e.g. System.Core issue: 
                    // http://stackoverflow.com/questions/18793959/filenotfoundexception-when-trying-to-load-autofac-as-an-embedded-assembly
                    if (name.EndsWith("Retargetable=Yes")) return Assembly.Load(asmName);

                    var searchAssemblies = new[] { thisAssembly }
                        .Select(a => Tuple.Create(a, a.GetManifestResourceNames()))
                        .ToList();

                    var resource = FindResource(asmName, new[] { ".dll" }, searchAssemblies);
                    if (resource == null) return null;

                    Assembly assembly = null;

                    byte[] assemblyData = LoadResourceBytes(resource);
                    var symbolResource = FindResource(asmName, new[] { ".pdb" }, searchAssemblies);

                    if (symbolResource != null)
                    {
                        byte[] symbolsData = LoadResourceBytes(symbolResource);

                        Trace.WriteLine(
                            string.Format(
                                "Loading '{0}' as embedded resource from '{1}' with symbols '{2}'",
                                resource.Item2,
                                resource.Item1,
                                symbolResource.Item2));

                        assembly = Assembly.Load(assemblyData, symbolsData);
                    }
                    else
                    {
                        Trace.WriteLine(
                            string.Format(
                                "Loading '{0}' as embedded resource from '{1}'",
                                resource.Item2,
                                resource.Item1));

                        assembly = Assembly.Load(assemblyData);
                    }

                    return assembly;
                }
                catch (Exception ex)
                {
                    LogUnhandledException("Failure Resolving Assemblies During Load", ex);
                    throw;
                }
            };
        }

        static void LogUnhandledException(string message, Exception ex)
        {
            var e = new List<string>(10)
            {
                string.Format("Message: {0}", message),
                string.Format("Exception: {0}", ex.ToString()),
                string.Format("Stack Trace: {0}", ex.StackTrace)
            };

            if (ex.InnerException != null)
            {
                e.Add(new string('-', 20));
                e.Add(string.Format("Inner Exception: {0}", ex.InnerException));
                e.Add(string.Format("Inner Stack Trace: {0}", ex.InnerException.StackTrace));
            }

            var loadingException = string.Join(Environment.NewLine, e);

            try
            {
                // attempt log to application
                EventLog.WriteEntry("Papercut", loadingException, EventLogEntryType.Error);
            }
            catch (Exception)
            {
            }
        }

        public static Tuple<Assembly, string> FindResource(AssemblyName asmName,
            string[] validExtensions,
            IList<Tuple<Assembly, string[]>> searchAssemblies)
        {
            List<string> possibleResourceNames =
                validExtensions.Select(ext => string.Format("{0}{1}", asmName.Name, ext)).ToList();

            return searchAssemblies.Select(
                assembly =>
                new
                {
                    assembly,
                    resourceName = assembly.Item2.FirstOrDefault(n => possibleResourceNames.Any(n.Contains))
                })
                .Where(_ => _.resourceName != null)
                .Select(_ => Tuple.Create(_.assembly.Item1, _.resourceName)).FirstOrDefault();
        }

        public static byte[] LoadResourceBytes(Tuple<Assembly, string> resource)
        {
            return LoadResourceBytes(resource.Item1, resource.Item2);
        }

        public static byte[] LoadResourceBytes(Assembly assembly, string resourceName)
        {
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                var assemblyData = new byte[stream.Length];
                stream.Read(assemblyData, 0, assemblyData.Length);
                return assemblyData;
            }
        }
    }
}