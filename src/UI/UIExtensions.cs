/*  
 * Papercut
 *
 *  Copyright © 2008 - 2012 Ken Robertson
 *  Copyright © 2013 - 2014 Jaben Cargman
 *  
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *  
 *  http://www.apache.org/licenses/LICENSE-2.0
 *  
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *  
 */
namespace Papercut.UI
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    using Papercut.Annotations;

    public static class UIExtensions
    {
        public static T GetObjectDataFromPoint<T>([NotNull] this ListBox source, Point point)
            where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            var element = source.InputHitTest(point) as UIElement;
            if (element == null)
            {
                return null;
            }

            // Get the object from the element
            object data = DependencyProperty.UnsetValue;

            while (data == DependencyProperty.UnsetValue)
            {
                // Try to get the object value for the corresponding element
                data = source.ItemContainerGenerator.ItemFromContainer(element);

                // Get the parent and we will iterate again
                if (data == DependencyProperty.UnsetValue && element != null)
                {
                    element = VisualTreeHelper.GetParent(element) as UIElement;
                }

                // If we reach the actual listbox then we must break to avoid an infinite loop
                if (Equals(element, source))
                {
                    return null;
                }
            }

            return data as T;
        }
    }
}