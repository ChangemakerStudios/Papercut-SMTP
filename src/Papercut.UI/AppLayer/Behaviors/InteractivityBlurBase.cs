﻿// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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
using System.Windows.Media.Effects;

using Microsoft.Xaml.Behaviors;

namespace Papercut.AppLayer.Behaviors
{
    public class InteractivityBlurBase<T> : Behavior<T>
        where T : DependencyObject
    {
        // Using a DependencyProperty as the backing store for BlurRadius. This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BlurRadiusProperty =
            DependencyProperty.Register(
                $"BlurRadius_{typeof(T).Name}",
                typeof(int),
                typeof(FrameworkElement),
                new UIPropertyMetadata(0));

        public int BlurRadius
        {
            get => (int) this.GetValue(BlurRadiusProperty);
            set => this.SetValue(BlurRadiusProperty, value);
        }

        protected virtual BlurEffect GetBlurEffect()
        {
            return new BlurEffect { Radius = this.BlurRadius };
        }
    }
}