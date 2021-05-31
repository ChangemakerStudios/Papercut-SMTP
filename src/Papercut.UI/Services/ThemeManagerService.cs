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


namespace Papercut.Services
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;

    using ControlzEx.Theming;

    using Papercut.Common.Domain;
    using Papercut.Core.Infrastructure.Lifecycle;
    using Papercut.Events;
    using Papercut.Properties;

    using Serilog;

    public class ThemeManagerService : IEventHandler<PapercutClientPreStartEvent>, IEventHandler<SettingsUpdatedEvent>
    {
        private readonly ILogger _logger;

        public ThemeManagerService(ILogger logger)
        {
            this._logger = logger;
        }

        private static ThemeManager CurrentTheme => ThemeManager.Current;

        public void Handle(PapercutClientPreStartEvent @event)
        {
            this.SetTheme();
        }

        public void Handle(SettingsUpdatedEvent @event)
        {
            if (@event.PreviousSettings.Theme == @event.NewSettings.Theme) return;

            this.SetTheme();
        }

        private void SetTheme()
        {
            var prop = typeof(Colors).GetProperties().FirstOrDefault(
                s =>
                    s.Name.Equals(Settings.Default.Theme, StringComparison.OrdinalIgnoreCase));

            if (prop == null)
            {
                this._logger.Warning("Unable to find theme color {ThemeColor}. Setting to default: LightBlue.", Settings.Default.Theme);
                Settings.Default.Theme = "LightBlue";
                return;
            }

            var themeColor = (Color)prop.GetValue(null);

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
    }
}