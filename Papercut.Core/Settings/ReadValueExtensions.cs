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

namespace Papercut.Core.Settings
{
    using System;

    using Papercut.Core.Annotations;
    using Papercut.Core.Helper;

    public static class ReadValueExtensions
    {
        public static T GetAs<T>(
            [NotNull] this IReadValue<string> readValue,
            [NotNull] string key,
            [NotNull] Func<T> getDefaultValue)
        {
            if (readValue == null) throw new ArgumentNullException("readValue");
            if (key == null) throw new ArgumentNullException("key");
            if (getDefaultValue == null) throw new ArgumentNullException("getDefaultValue");

            string value = readValue.Get(key);
            return value.IsDefault() ? getDefaultValue() : value.ToType<T>();
        }

        public static T GetAs<T>(
            [NotNull] this IReadValue<string> readValue,
            [NotNull] string key,
            [CanBeNull] T defaultValue = default(T))
        {
            if (readValue == null) throw new ArgumentNullException("readValue");
            if (key == null) throw new ArgumentNullException("key");

            string value = readValue.Get(key);
            return value.IsDefault() ? defaultValue : value.ToType<T>();
        }
    }
}