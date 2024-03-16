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


using Autofac;

using Papercut.Domain.LifecycleHooks;

namespace Papercut.AppLayer.Settings
{
    public class SaveSettingsOnExitService : IAppLifecyclePreExit
    {
        readonly ILogger _logger;

        public SaveSettingsOnExitService(ILogger logger)
        {
            this._logger = logger;
        }

        public Task<AppLifecycleActionResultType> OnPreExit()
        {
            try
            {
                if (Properties.Settings.Default.MainWindowHeight < 300)
                {
                    Properties.Settings.Default.MainWindowHeight = 300;
                }
                if (Properties.Settings.Default.MainWindowWidth < 400)
                {
                    Properties.Settings.Default.MainWindowWidth = 400;
                }

                this._logger.Debug("Saving Updated Settings...");
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                this._logger.Error(ex, "Failure Saving Settings File");
            }

            return Task.FromResult(AppLifecycleActionResultType.Continue);
        }

        #region Begin Static Container Registrations

        /// <summary>
        /// Called dynamically from the RegisterStaticMethods() call in the container module.
        /// </summary>
        /// <param name="builder"></param>
        [UsedImplicitly]
        static void Register(ContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.RegisterType<SaveSettingsOnExitService>().AsImplementedInterfaces()
                .SingleInstance();
        }

        #endregion
    }
}