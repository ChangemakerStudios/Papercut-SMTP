// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
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


using ControlzEx.Theming;

using Microsoft.Win32;

using Papercut.Core.Infrastructure.Async;
using Papercut.Domain.Themes;
using Papercut.Infrastructure.Themes;

using ReactiveUI;

using IMessageBus = Papercut.Common.Domain.IMessageBus;

namespace Papercut.AppLayer.Themes;

public class ThemeManagerService(
    ILogger logger,
    ThemeColorRepository themeColorRepository, IMessageBus messageBus)
    : Disposable, IAppLifecyclePreStart, IEventHandler<SettingsUpdatedEvent>
{
    private bool _lastDarkModeState;

    private System.Windows.Media.Color _lastThemeColorState;

    private IDisposable? _monitoringSubscription;

    private static ThemeManager CurrentTheme => ThemeManager.Current;

    public async Task<AppLifecycleActionResultType> OnPreStart()
    {
        await SetTheme(false);
        SetupThemeMonitoringObservable();

        return AppLifecycleActionResultType.Continue;
    }

    public async Task HandleAsync(SettingsUpdatedEvent @event, CancellationToken token)
    {
        var themeChanged = @event.PreviousSettings.Theme != @event.NewSettings.Theme;
        var baseThemeChanged = @event.PreviousSettings.BaseTheme != @event.NewSettings.BaseTheme;

        if (themeChanged || baseThemeChanged)
        {
            await SetTheme();
        }
    }

    private bool IsSystemThemeMode()
    {
        return Enum.TryParse<BaseTheme>(Properties.Settings.Default.BaseTheme, out var baseTheme)
            && baseTheme == BaseTheme.System;
    }

    private async Task SetTheme(bool sendThemeChangedEvent = true)
    {
        var colorTheme = themeColorRepository.FirstOrDefaultByName(Properties.Settings.Default.Theme);

        if (colorTheme == null)
        {
            logger.Warning(
                "Unable to find theme accent color {ThemeColor}. Setting to default: {DefaultTheme}.",
                Properties.Settings.Default.Theme, ThemeColorRepository.Default.Name);

            Properties.Settings.Default.Theme = ThemeColorRepository.Default.Name;
            return;
        }

        var themeColor = colorTheme.Color;

        // Determine if we should use dark mode
        var isDarkMode = GetCurrentIsDarkMode();
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

        bool hasThemeColorChanged = themeColor != _lastThemeColorState;
        bool hasDarkModeChanged = isDarkMode != _lastDarkModeState;

        _lastDarkModeState = isDarkMode;
        _lastThemeColorState = themeColor;

        if (sendThemeChangedEvent && (hasThemeColorChanged || hasDarkModeChanged))
        {
            await messageBus.PublishAsync(new ThemeChangedEvent(isDarkMode, themeColor));
        }

        Application.Current?.MainWindow?.Activate();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing) return;

        if (_monitoringSubscription is null) return;

        logger.Debug("Stopping system theme monitoring");
        _monitoringSubscription?.Dispose();
        _monitoringSubscription = null;
    }

    private void SetupThemeMonitoringObservable()
    {
        logger.Information("Current system theme at startup: {Theme}", SystemThemeRegistryHelper.IsSystemDarkMode() ? "Dark" : "Light");

        _monitoringSubscription = Observable
            .FromEventPattern<UserPreferenceChangedEventHandler, UserPreferenceChangedEventArgs>(
                h => SystemEvents.UserPreferenceChanged += h,
                h => SystemEvents.UserPreferenceChanged -= h)
            .Where(e => e.EventArgs.Category == UserPreferenceCategory.General)
            .Select(_ => SystemThemeRegistryHelper.IsSystemDarkMode())
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler)
            .SubscribeAsync(
                async isDarkMode =>
                {
                    logger.Information("System theme changed, updating application theme to: {Theme}", isDarkMode ? "Dark" : "Light");

                    if (IsSystemThemeMode())
                    {
                        await SetTheme(true);
                    }
                },
                ex => logger.Warning(ex, "Error checking system theme change"));
    }

    private bool GetCurrentIsDarkMode()
    {
        if (!Enum.TryParse<BaseTheme>(Properties.Settings.Default.BaseTheme, out var baseTheme))
        {
            logger.Warning("Unable to parse BaseTheme setting: {BaseTheme}. Defaulting to System.", Properties.Settings.Default.BaseTheme);
            baseTheme = BaseTheme.System;
        }

        return baseTheme switch
        {
            BaseTheme.System => SystemThemeRegistryHelper.IsSystemDarkMode(),
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