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


using System.Windows;
using System.Windows.Data;

namespace Papercut.AppLayer.Behaviors;

/// <summary>
/// Very useful code from here:
/// http://tomlev2.wordpress.com/2008/11/18/wpf-binding-to-application-settings-using-a-markup-extension/
///
/// Modified to prevent saving window dimensions when minimized (Issue #327)
/// </summary>
public class SettingBindingExtension : Binding
{
    public SettingBindingExtension()
    {
        this.Initialize();
    }

    public SettingBindingExtension(string path)
        : base(path)
    {
        this.Initialize();
    }

    void Initialize()
    {
        this.Source = Properties.Settings.Default;
        this.Mode = BindingMode.TwoWay;

        // Use a value converter to prevent saving dimensions when window is minimized
        this.Converter = WindowDimensionConverter.Instance;
    }
}

/// <summary>
/// Converter that prevents saving window dimensions when the window is minimized.
/// This fixes issue #327 where minimized windows would save width/height as zero.
/// </summary>
internal class WindowDimensionConverter : IValueConverter
{
    public static readonly WindowDimensionConverter Instance = new();

    private Window? _cachedWindow;

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        // Setting -> Window: pass through the saved value
        return value;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        // Window -> Setting: only save if window is not minimized

        // Try to get window from Application.Current.MainWindow as a fallback
        _cachedWindow ??= Application.Current?.MainWindow;

        // Don't save dimensions if window is minimized
        if (_cachedWindow?.WindowState == WindowState.Minimized)
        {
            return Binding.DoNothing;
        }

        // Otherwise, save the value
        return value;
    }
}