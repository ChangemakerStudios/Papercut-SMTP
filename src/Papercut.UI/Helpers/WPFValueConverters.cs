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
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    /// <summary>
    ///     Source: http://stackoverflow.com/a/1039681
    /// </summary>
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (targetType != typeof(bool)) throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

    /// <summary>
    ///     Converts Boolean Values to Control.Visibility values.
    ///     Source: http://www.codeproject.com/Tips/285358/All-purpose-Boolean-to-Visibility-Converter
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        //Set to true if you want to show control when boolean value is true
        //Set to false if you want to hide/collapse control when value is true

        //Set to true if you just want to hide the control
        //else set to false if you want to collapse the control

        public BooleanToVisibilityConverter()
        {
            TriggerValue = false;
        }

        public bool TriggerValue { get; set; }

        public bool IsHidden { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool booleanValue = value != null;

            if (value is string) booleanValue = !string.IsNullOrEmpty(value as string);
            else if (value is bool) booleanValue = (bool)value;

            return GetVisibility(booleanValue);
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        object GetVisibility(bool toggleValue)
        {
            if ((toggleValue && TriggerValue && IsHidden)
                || (!toggleValue && !TriggerValue && IsHidden)) return Visibility.Hidden;
            if ((toggleValue && TriggerValue && !IsHidden)
                || (!toggleValue && !TriggerValue && !IsHidden)) return Visibility.Collapsed;
            return Visibility.Visible;
        }
    }
}