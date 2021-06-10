// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2021 Jaben Cargman
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


namespace Papercut.AppLayer.Settings
{
    using System;

    using Autofac;

    using Papercut.Core.Annotations;
    using Papercut.Domain.LifecycleHooks;
    using Papercut.Properties;

    using Serilog;

    public class SaveSettingsOnExitService : IAppLifecyclePreExit
    {
        readonly ILogger _logger;

        public SaveSettingsOnExitService(ILogger logger)
        {
            this._logger = logger;
        }

        public AppLifecycleActionResultType OnPreExit()
        {
            try
            {
                if (Settings.Default.Window_Height < 300)
                {
                    Settings.Default.Window_Height = 300;
                }
                if (Settings.Default.Window_Width < 400)
                {
                    Settings.Default.Window_Width = 400;
                }

                this._logger.Debug("Saving Updated Settings...");
                Settings.Default.Save();
            }
            catch (Exception ex)
            {
                this._logger.Error(ex, "Failure Saving Settings File");
            }

            return AppLifecycleActionResultType.Continue;
        }

        #region Begin Static Container Registrations

        /// <summary>
        /// Called dynamically from the RegisterStaticMethods() call in the container module.
        /// </summary>
        /// <param name="builder"></param>
        [UsedImplicitly]
        static void Register([NotNull] ContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.RegisterType<SaveSettingsOnExitService>().AsImplementedInterfaces()
                .SingleInstance();
        }

        #endregion
    }
}