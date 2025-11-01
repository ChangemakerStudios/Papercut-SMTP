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


using Microsoft.Win32;

using Papercut.Core.Infrastructure.Async;

using ReactiveUI;

using IMessageBus = Papercut.Common.Domain.IMessageBus;

namespace Papercut.AppLayer.Themes;

/// <summary>
/// Monitors Windows system theme changes and reports whether the system is using light or dark theme
/// </summary>
public class SystemThemeMonitor(IMessageBus messageBus, ILogger logger) : Disposable, IAppLifecycleStarted
{
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private const string RegistryValueName = "AppsUseLightTheme";

    private IDisposable? _monitoringSubscription;

    public Task OnStartedAsync()
    {
        SetupThemeMonitoringObservable();

        return Task.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            if (_monitoringSubscription is not null)
            {
                logger.Debug("Stopping system theme monitoring");
                _monitoringSubscription?.Dispose();
                _monitoringSubscription = null;
            }
        }
    }

    public bool IsDarkMode() => !IsLightMode();

    private void SetupThemeMonitoringObservable()
    {
        logger.Information("Current system theme at startup: {Theme}", IsDarkMode() ? "Dark" : "Light");

        _monitoringSubscription = Observable
            .FromEventPattern<UserPreferenceChangedEventHandler, UserPreferenceChangedEventArgs>(
                h => SystemEvents.UserPreferenceChanged += h,
                h => SystemEvents.UserPreferenceChanged -= h)
            .Where(e => e.EventArgs.Category == UserPreferenceCategory.General)
            .Select(_ => IsDarkMode())
            .DistinctUntilChanged()
            .ObserveOn(RxApp.TaskpoolScheduler)
            .SubscribeAsync(
                async isDarkMode =>
                {
                    await messageBus.PublishAsync(new SystemThemeChangedEvent(isDarkMode));
                },
                ex => logger.Warning(ex, "Error checking system theme change"));
    }

    public bool IsLightMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            var value = key?.GetValue(RegistryValueName);

            if (value is int intValue)
            {
                return intValue == 1; // 1 = Light theme, 0 = Dark theme
            }

            // Default to light theme if registry key doesn't exist
            return true;
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to read current system theme from registry");

            // Default to light theme on error
            return true;
        }
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

        builder.RegisterType<SystemThemeMonitor>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
    }

    #endregion
}
