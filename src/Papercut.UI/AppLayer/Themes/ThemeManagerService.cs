// Papercut
//
// Copyright � 2008 - 2012 Ken Robertson
// Copyright � 2013 - 2025 Jaben Cargman
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


namespace Papercut.AppLayer.Themes;

using ControlzEx.Theming;

using Papercut.Infrastructure.Themes;

public class ThemeManagerService(ILogger logger, ThemeColorRepository themeColorRepository)
    : IAppLifecyclePreStart, IEventHandler<SettingsUpdatedEvent>
{
    private static ThemeManager CurrentTheme => ThemeManager.Current;

    public Task<AppLifecycleActionResultType> OnPreStart()
    {
        SetTheme();
        return Task.FromResult(AppLifecycleActionResultType.Continue);
    }

    public Task HandleAsync(SettingsUpdatedEvent @event, CancellationToken token)
    {
        if (@event.PreviousSettings.Theme != @event.NewSettings.Theme
            || @event.PreviousSettings.DarkMode != @event.NewSettings.DarkMode)
        {
            SetTheme();
        }

        return Task.CompletedTask;
    }

    private void SetTheme()
    {
        var colorTheme = themeColorRepository.FirstOrDefaultByName(Properties.Settings.Default.Theme);

        if (colorTheme == null)
        {
            logger.Warning(
                "Unable to find theme color {ThemeColor}. Setting to default: {DefaultTheme}.",
                Properties.Settings.Default.Theme, ThemeColorRepository.Default.Name);

            Properties.Settings.Default.Theme = ThemeColorRepository.Default.Name;
            return;
        }

        var themeColor = colorTheme.Color;
        var isDarkMode = Properties.Settings.Default.DarkMode;
        var baseColorScheme = isDarkMode ? "Dark" : "Light";

        var generateRuntimeTheme = RuntimeThemeGenerator.Current.GenerateRuntimeTheme(baseColorScheme, themeColor);
        if (generateRuntimeTheme != null)
        {
            CurrentTheme.AddTheme(generateRuntimeTheme);
            CurrentTheme.ChangeTheme(
                Application.Current,
                generateRuntimeTheme
            );
        }
        else
        {
            logger.Error("Failed to generate theme for {Scheme}.{Color} -- theme not changed", baseColorScheme, themeColor);
        }

        Application.Current?.MainWindow?.Activate();
    }

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    private static void Register(ContainerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RegisterType<ThemeManagerService>().AsImplementedInterfaces();
    }

    #endregion
}