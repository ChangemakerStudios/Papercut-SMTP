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

namespace Papercut.AppLayer.Themes;

/// <summary>
/// Monitors Windows system theme changes and reports whether the system is using light or dark theme
/// </summary>
public class SystemThemeMonitor : IDisposable
{
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string RegistryValueName = "AppsUseLightTheme";
    private readonly ILogger _logger;
    private readonly SynchronizationContext _syncContext;
    private Timer? _pollingTimer;

    public SystemThemeMonitor(ILogger logger)
    {
        _logger = logger;
        _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
    }

    public event EventHandler<bool>? SystemThemeChanged;

    public bool IsSystemDarkMode => !IsLightThemeEnabled();

    public void StartMonitoring(TimeSpan pollingInterval)
    {
        _logger.Information("Starting system theme monitoring with polling interval: {Interval}", pollingInterval);

        var currentTheme = IsSystemDarkMode;
        _logger.Information("Current system theme at startup: {Theme}", currentTheme ? "Dark" : "Light");

        // Use polling to check for theme changes
        _pollingTimer = new Timer(
            _ => CheckThemeChange(),
            null,
            pollingInterval,
            pollingInterval);
    }

    public void StopMonitoring()
    {
        _logger.Information("Stopping system theme monitoring");
        _pollingTimer?.Dispose();
        _pollingTimer = null;
    }

    private bool _lastKnownDarkMode;
    private bool _isFirstCheck = true;

    private void CheckThemeChange()
    {
        try
        {
            var isDarkMode = IsSystemDarkMode;

            if (_isFirstCheck)
            {
                _lastKnownDarkMode = isDarkMode;
                _isFirstCheck = false;
                return;
            }

            if (isDarkMode != _lastKnownDarkMode)
            {
                _logger.Information("System theme changed to: {Theme}", isDarkMode ? "Dark" : "Light");
                _lastKnownDarkMode = isDarkMode;

                // Raise event on UI thread
                _syncContext.Post(_ => SystemThemeChanged?.Invoke(this, isDarkMode), null);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error checking system theme change");
        }
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
