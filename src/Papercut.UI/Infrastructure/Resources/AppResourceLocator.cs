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


using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Resources;

using Autofac;

namespace Papercut.Infrastructure.Resources
{
    public class AppResourceLocator
    {
        readonly string _appExecutableName;

        readonly ILogger _logger;

        public AppResourceLocator(ILogger logger)
        {
            this._logger = logger.ForContext<AppResourceLocator>();
            this._appExecutableName =
                Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
        }

        public string GetResourceString(string resourceName)
        {
            ArgumentNullException.ThrowIfNull(resourceName);

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
                            $"/{this._appExecutableName};component/{resourceName}",
                            UriKind.Relative));
            }
            catch (Exception ex)
            {
                this._logger.Error(
                    ex,
                    "Failure loading application resource {ResourceName} in {ExecutableName}",
                    resourceName,
                    this._appExecutableName);

                throw;
            }
        }

        #region Begin Static Container Registrations

        /// <summary>
        /// Called dynamically from the RegisterStaticMethods() call in the container module.
        /// </summary>
        /// <param name="builder"></param>
        [UsedImplicitly]
        static void Register(ContainerBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.RegisterType<AppResourceLocator>().AsSelf().InstancePerLifetimeScope();
        }

        #endregion
    }
}