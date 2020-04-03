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

namespace Papercut.Common.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Papercut.Common.Helper;

    public static class EnumerableExtensions
    {
        public static IEnumerable<T> IfNullEmpty<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
                return Enumerable.Empty<T>();

            return enumerable;
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.IfNullEmpty().Where(s => s != null);
        }

        public static IEnumerable<string> ToFormattedPairs(this IEnumerable<KeyValuePair<string, Lazy<object>>> keyValuePairs)
        {
            return keyValuePairs.IfNullEmpty().Select(s => KeyValuePair.Create(s.Key, $"{s.Value.Value}"))
                .Where(s => s.Value.IsSet())
                .Select(s => $"{s.Key}: {s.Value}");
        }
    }
}