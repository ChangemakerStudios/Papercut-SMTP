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


using System.Reactive.Concurrency;
using System.Reactive.Linq;

using Microsoft.Win32;

using ReactiveUI;

namespace Papercut.AppLayer.Themes;

/// <summary>
/// Monitors Windows system theme changes and reports whether the system is using light or dark theme
/// </summary>
public class SystemThemeMonitor : IDisposable
{
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string RegistryValueName = "AppsUseLightTheme";
    private readonly ILogger _logger;
    private IDisposable? _monitoringSubscription;

    public SystemThemeMonitor(ILogger logger)
    {
        _logger = logger;
    }

    public event EventHandler<bool>? SystemThemeChanged;

    public bool IsSystemDarkMode => !IsLightThemeEnabled();

    public void StartMonitoring(TimeSpan pollingInterval)
    {
        _logger.Information("Starting system theme monitoring with polling interval: {Interval}", pollingInterval);

        var currentTheme = IsSystemDarkMode;
        _logger.Information("Current system theme at startup: {Theme}", currentTheme ? "Dark" : "Light");

        // Use Rx to poll for theme changes
        _monitoringSubscription = Observable
            .Interval(pollingInterval)
            .Select(_ => IsSystemDarkMode)
            .DistinctUntilChanged()
            .Skip(1) // Skip the first value since we already logged the initial theme
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(
                isDarkMode =>
                {
                    _logger.Information("System theme changed to: {Theme}", isDarkMode ? "Dark" : "Light");
                    SystemThemeChanged?.Invoke(this, isDarkMode);
                },
                ex => _logger.Warning(ex, "Error checking system theme change"));
    }

    public void StopMonitoring()
    {
        _logger.Information("Stopping system theme monitoring");
        _monitoringSubscription?.Dispose();
        _monitoringSubscription = null;
    }

    private static bool IsLightThemeEnabled()
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
        catch
        {
            // Default to light theme on error
            return true;
        }
    }

    public void Dispose()
    {
        StopMonitoring();
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

        builder.RegisterType<SystemThemeMonitor>().AsSelf().SingleInstance();
    }

    #endregion
}
