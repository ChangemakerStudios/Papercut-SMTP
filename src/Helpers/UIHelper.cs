// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2014 Jaben Cargman
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
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    using Papercut.Core.Annotations;
    using System.Runtime.InteropServices;
    using MahApps.Metro.Controls;

    public static class UIHelper
    {
        [DllImport("dwmapi.dll")]
        static extern IntPtr DwmIsCompositionEnabled(out bool pfEnabled);

        public static void Add2dBorders(MetroWindow w)
        {
            bool aeroEnabled = false;
            try
            {
                if (DwmIsCompositionEnabled(out aeroEnabled) == IntPtr.Zero)
                {
                    // No Need to to anything here
                }
            }
            catch
            {
                // No Need to to anything here, may be older OS
            }

            if (!aeroEnabled)
            {
                w.Resources.Add("2dThick", new System.Windows.Thickness(3));
                w.Resources.Add("2dBrush", new SolidColorBrush(Color.FromRgb(66, 178, 231)));
            }
        }

        public static object GetObjectDataFromPoint([NotNull] this ListBox source, Point point)
        {
            if (source == null) throw new ArgumentNullException("source");

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
            if (dependencyObject == null) throw new ArgumentNullException("dependencyObject");

            DependencyObject parent = VisualTreeHelper.GetParent(dependencyObject);
            if (parent == null) return null;
            var parentT = parent as T;
            return parentT ?? FindAncestor<T>(parent);
        }
    }
}