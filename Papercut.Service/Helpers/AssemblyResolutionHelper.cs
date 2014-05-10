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

namespace Papercut.Service.Helpers
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
            string[] validExtensions = { ".dll" };
            string[] resourceNames = thisAssembly.GetManifestResourceNames();

            // Code based on: http://www.codingmurmur.com/2014/02/embedded-assembly-loading-with-support.html
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string name = args.Name;
                var asmName = new AssemblyName(name);

                // Any retargetable assembly should be resolved directly using normal load e.g. System.Core issue: 
                // http://stackoverflow.com/questions/18793959/filenotfoundexception-when-trying-to-load-autofac-as-an-embedded-assembly
                if (name.EndsWith("Retargetable=Yes")) return Assembly.Load(asmName);

                List<string> possibleResourceNames =
                    validExtensions.Select(ext => string.Format("{0}{1}", asmName.Name, ext))
                        .ToList();
                string resourceToFind = string.Join(",", possibleResourceNames);
                string resourceName =
                    resourceNames.FirstOrDefault(n => possibleResourceNames.Any(n.Contains));

                if (string.IsNullOrWhiteSpace(resourceName)) return null;

                string symbolsToFind = asmName.Name + ".pdb";
                string symbolsName = resourceNames.SingleOrDefault(n => n.Contains(symbolsToFind));

                byte[] assemblyData = LoadResourceBytes(thisAssembly, resourceName);

                if (string.IsNullOrWhiteSpace(symbolsName))
                {
                    Trace.WriteLine(
                        string.Format(
                            "Loading '{0}' as embedded resource '{1}'",
                            resourceToFind,
                            resourceName));

                    return Assembly.Load(assemblyData);
                }

                byte[] symbolsData = LoadResourceBytes(thisAssembly, symbolsName);

                Trace.WriteLine(
                    string.Format(
                        "Loading '{0}' as embedded resource '{1}' with symbols '{2}'",
                        resourceToFind,
                        resourceName,
                        symbolsName));

                return Assembly.Load(assemblyData, symbolsData);
            };
        }

        public static byte[] LoadResourceBytes(Assembly executingAssembly, string resourceName)
        {
            using (Stream stream = executingAssembly.GetManifestResourceStream(resourceName))
            {
                var assemblyData = new byte[stream.Length];
                stream.Read(assemblyData, 0, assemblyData.Length);
                return assemblyData;
            }
        }
    }
}