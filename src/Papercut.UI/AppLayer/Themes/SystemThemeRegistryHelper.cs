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


using System.Windows.Media;

using Microsoft.Win32;

namespace Papercut.AppLayer.Themes;

public static class SystemThemeRegistryHelper
{
    private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private const string AppsUseLightThemeValue = "AppsUseLightTheme";

    private const string DwmKeyPath = @"Software\Microsoft\Windows\DWM";

    private const string ColorizationColorValue = "ColorizationColor";

    internal static bool IsSystemDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath);
            var value = key?.GetValue(AppsUseLightThemeValue);

            if (value is int intValue)
            {
                return intValue == 0; // 1 = Light theme, 0 = Dark theme
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to read current system theme from registry");
        }

        // Default to light theme on error
        return false;
    }

    internal static Color? GetSystemAccentColor()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(DwmKeyPath);
            var value = key?.GetValue(ColorizationColorValue);

            if (value is int intValue)
            {
                var argb = unchecked((uint)intValue);
                return Color.FromArgb(
                    byte.MaxValue, // Force full opacity
                    (byte)((argb >> 16) & 0xFF),
                    (byte)((argb >> 8) & 0xFF),
                    (byte)(argb & 0xFF));
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to read system accent color from registry");
        }

        return null;
    }
}