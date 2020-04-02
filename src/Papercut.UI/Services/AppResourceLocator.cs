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

namespace Papercut.Services
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Windows;
    using System.Windows.Resources;

    using Papercut.Core.Annotations;

    using Serilog;

    public class AppResourceLocator
    {
        readonly string _appExecutableName;

        readonly ILogger _logger;

        public AppResourceLocator(ILogger logger)
        {
            _logger = logger.ForContext<AppResourceLocator>();
            _appExecutableName =
                Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
        }

        public string GetResourceString([NotNull] string resourceName)
        {
            if (resourceName == null)
                throw new ArgumentNullException(nameof(resourceName));

            var resource =
                Assembly.GetExecutingAssembly()
                    .GetManifestResourceNames()
                    .FirstOrDefault(s => s.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));

            using (
                var streamReader = new StreamReader(
                    Assembly.GetExecutingAssembly().GetManifestResourceStream(resource),
                    Encoding.Default))
            {
                return streamReader.ReadToEnd();
            }
        }

        public StreamResourceInfo GetResource(string resourceName)
        {
            try
            {
                return
                    Application.GetResourceStream(
                        new Uri(
                            string.Format("/{0};component/{1}", _appExecutableName, resourceName),
                            UriKind.Relative));
            }
            catch (Exception ex)
            {
                _logger.Error(
                    ex,
                    "Failure loading application resource {ResourceName} in {ExecutableName}",
                    resourceName,
                    _appExecutableName);

                throw;
            }
        }
    }
}