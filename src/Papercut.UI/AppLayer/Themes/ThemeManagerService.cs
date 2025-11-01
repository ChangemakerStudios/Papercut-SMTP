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

using Papercut.Domain.Themes;
using Papercut.Infrastructure.Themes;

public class ThemeManagerService(
    ILogger logger,
    ThemeColorRepository themeColorRepository,
    SystemThemeMonitor systemThemeMonitor)
    : IAppLifecyclePreStart, IAppLifecyclePreExit, IEventHandler<SettingsUpdatedEvent>
{
    private static ThemeManager CurrentTheme => ThemeManager.Current;

    public Task<AppLifecycleActionResultType> OnPreStart()
    {
        // Subscribe to system theme changes
        systemThemeMonitor.SystemThemeChanged += OnSystemThemeChanged;

        // Start monitoring if set to System mode
        if (IsSystemThemeMode())
        {
            systemThemeMonitor.StartMonitoring(TimeSpan.FromSeconds(2));
        }

        SetTheme();
        return Task.FromResult(AppLifecycleActionResultType.Continue);
    }

    public Task<AppLifecycleActionResultType> OnPreExit()
    {
        systemThemeMonitor.SystemThemeChanged -= OnSystemThemeChanged;
        systemThemeMonitor.StopMonitoring();
        return Task.FromResult(AppLifecycleActionResultType.Continue);
    }

    public Task HandleAsync(SettingsUpdatedEvent @event, CancellationToken token)
    {
        var themeChanged = @event.PreviousSettings.Theme != @event.NewSettings.Theme;
        var baseThemeChanged = @event.PreviousSettings.BaseTheme != @event.NewSettings.BaseTheme;

        if (themeChanged || baseThemeChanged)
        {
            // Update monitoring based on new base theme setting
            if (IsSystemThemeMode())
            {
                systemThemeMonitor.StartMonitoring(TimeSpan.FromSeconds(2));
            }
            else
            {
                systemThemeMonitor.StopMonitoring();
            }

            SetTheme();
        }

        return Task.CompletedTask;
    }

    private void OnSystemThemeChanged(object? sender, bool isDarkMode)
    {
        if (IsSystemThemeMode())
        {
            logger.Information("System theme changed, updating application theme to: {Theme}", isDarkMode ? "Dark" : "Light");
            SetTheme();
        }
    }

    private bool IsSystemThemeMode()
    {
        return Enum.TryParse<BaseTheme>(Properties.Settings.Default.BaseTheme, out var baseTheme)
            && baseTheme == BaseTheme.System;
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

        // Determine if we should use dark mode
        var isDarkMode = ShouldUseDarkMode();
        var baseColorScheme = isDarkMode ? "Dark" : "Light";

        logger.Debug("Applying theme: BaseScheme={BaseScheme}, AccentColor={AccentColor}", baseColorScheme, themeColor);

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

    private bool ShouldUseDarkMode()
    {
        if (!Enum.TryParse<BaseTheme>(Properties.Settings.Default.BaseTheme, out var baseTheme))
        {
            logger.Warning("Unable to parse BaseTheme setting: {BaseTheme}. Defaulting to System.", Properties.Settings.Default.BaseTheme);
            baseTheme = BaseTheme.System;
        }

        return baseTheme switch
        {
            BaseTheme.System => systemThemeMonitor.IsSystemDarkMode,
            BaseTheme.Dark => true,
            BaseTheme.Light => false,
            _ => false
        };
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