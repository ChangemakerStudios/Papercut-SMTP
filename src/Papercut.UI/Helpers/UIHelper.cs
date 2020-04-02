// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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

namespace Papercut.Helpers
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    using MahApps.Metro;
    using MahApps.Metro.Controls;

    using Papercut.Core.Annotations;

    public static class UIHelper
    {
        static readonly bool _isAeroEnabled;

        static UIHelper()
        {
            // figure out if Aero is enabled/disabled
            try
            {
                DwmIsCompositionEnabled(out _isAeroEnabled);
            }
            catch
            {
                // ignored
            }
        }

        [DllImport("dwmapi.dll")]
        static extern IntPtr DwmIsCompositionEnabled(out bool pfEnabled);

        public static void AutoAdjustBorders(this MetroWindow window)
        {
            var brushObjectName = "HighlightBrush";

            var appStyle = ThemeManager.DetectAppStyle(Application.Current);

            // Only add borders if above call succeded and Aero Not enabled
            var resourceDictionary = window.Resources;

            SetBorder(resourceDictionary, appStyle.Item2.Resources[brushObjectName]);

            ThemeManager.IsThemeChanged += (sender, args) =>
            {
                SetBorder(resourceDictionary, args.Accent.Resources[brushObjectName]);
            };
        }

        private static void SetBorder(ResourceDictionary resourceDictionary, object brush)
        {
            TryRemoveFromResourceDictionary(resourceDictionary, "AccentBorderThickness");
            TryRemoveFromResourceDictionary(resourceDictionary, "AccentBorderBrush");
            TryRemoveFromResourceDictionary(resourceDictionary, "AccentGlowBrush");

            if (_isAeroEnabled)
            {
                resourceDictionary.Add("AccentBorderThickness", new Thickness(1));
                resourceDictionary.Add("AccentGlowBrush", brush);
            }
            else
            {
                resourceDictionary.Add("AccentBorderThickness", new Thickness(3));
                resourceDictionary.Add("AccentBorderBrush", brush);
            }
        }

        private static bool TryRemoveFromResourceDictionary(ResourceDictionary resourceDictionary, string keyName)
        {
            if (!resourceDictionary.Contains(keyName)) return false;

            resourceDictionary.Remove(keyName);

            return true;
        }

        public static object GetObjectDataFromPoint([NotNull] this ListBox source, Point point)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var element = source.InputHitTest(point) as UIElement;
            if (element == null) return null;

            // Get the object from the element
            object data = DependencyProperty.UnsetValue;

            while (data == DependencyProperty.UnsetValue)
            {
                // Try to get the object value for the corresponding element
                data = source.ItemContainerGenerator.ItemFromContainer(element);

                // Get the parent and we will iterate again
                if (data == DependencyProperty.UnsetValue && element != null)
                    element = VisualTreeHelper.GetParent(element) as UIElement;

                // If we reach the actual listbox then we must break to avoid an infinite loop
                if (Equals(element, source)) return null;
            }

            return data;
        }

        public static T FindAncestor<T>(this DependencyObject dependencyObject)
            where T : DependencyObject
        {
            if (dependencyObject == null) throw new ArgumentNullException(nameof(dependencyObject));

            DependencyObject parent = VisualTreeHelper.GetParent(dependencyObject);
            if (parent == null) return null;
            var parentT = parent as T;
            return parentT ?? FindAncestor<T>(parent);
        }
    }
}