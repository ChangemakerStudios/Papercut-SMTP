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


using System.Windows;

using Autofac;

using ControlzEx.Theming;

using Papercut.Common.Domain;
using Papercut.Domain.Events;
using Papercut.Domain.LifecycleHooks;
using Papercut.Infrastructure.Themes;

namespace Papercut.AppLayer.Themes
{
    public class ThemeManagerService : IAppLifecyclePreStart, IEventHandler<SettingsUpdatedEvent>
    {
        private readonly ILogger _logger;

        private readonly ThemeColorRepository _themeColorRepository;

        public ThemeManagerService(ILogger logger, ThemeColorRepository themeColorRepository)
        {
            this._logger = logger;
            this._themeColorRepository = themeColorRepository;
        }

        private static ThemeManager CurrentTheme => ThemeManager.Current;

        public Task<AppLifecycleActionResultType> OnPreStart()
        {
            this.SetTheme();
            return Task.FromResult(AppLifecycleActionResultType.Continue);
        }

        public Task HandleAsync(SettingsUpdatedEvent @event, CancellationToken token)
        {
            if (@event.PreviousSettings.Theme != @event.NewSettings.Theme) this.SetTheme();

            return Task.CompletedTask;
        }

        private void SetTheme()
        {
            var colorTheme = this._themeColorRepository.FirstOrDefaultByName(Properties.Settings.Default.Theme);

            if (colorTheme == null)
            {
                this._logger.Warning("Unable to find theme color {ThemeColor}. Setting to default: LightBlue.", Properties.Settings.Default.Theme);
                Properties.Settings.Default.Theme = "LightBlue";
                return;
            }

            var themeColor = colorTheme.Color;

            var theme = CurrentTheme.DetectTheme(Application.Current);
            if (theme != null)
            {
                var inverseTheme = CurrentTheme.GetInverseTheme(theme);
                if (inverseTheme != null)
                {
                    var runtimeTheme = RuntimeThemeGenerator.Current.GenerateRuntimeTheme(inverseTheme.BaseColorScheme, themeColor);
                    if (runtimeTheme != null) CurrentTheme.AddTheme(runtimeTheme);
                }

                var generateRuntimeTheme = RuntimeThemeGenerator.Current.GenerateRuntimeTheme(theme.BaseColorScheme, themeColor);
                if (generateRuntimeTheme != null)
                    CurrentTheme.ChangeTheme(
                        Application.Current,
                        CurrentTheme.AddTheme(generateRuntimeTheme));
            }

            Application.Current?.MainWindow?.Activate();
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

            builder.RegisterType<ThemeManagerService>().AsImplementedInterfaces();
        }

        #endregion
    }
}