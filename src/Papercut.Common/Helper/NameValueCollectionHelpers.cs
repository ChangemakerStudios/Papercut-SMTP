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

namespace Papercut.Common.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;

    using Papercut.Core.Annotations;

    public static class NameValueCollectionExtensions
    {
        public static ILookup<string, string> ToLookup(
            [NotNull] this NameValueCollection nameValueCollection)
        {
            if (nameValueCollection == null) throw new ArgumentNullException(nameof(nameValueCollection));

            return nameValueCollection.ToKeyValuePairs().ToLookup(k => k.Key, v => v.Value);
        }

        public static IEnumerable<KeyValuePair<string, string>> ToKeyValuePairs(
            [NotNull] this NameValueCollection nameValueCollection)
        {
            if (nameValueCollection == null) throw new ArgumentNullException(nameof(nameValueCollection));

            return nameValueCollection.AllKeys.SelectMany(
                k =>
                {
                    string[] values = nameValueCollection.GetValues(k);

                    if (values != null && values.Any()) return values.Select(s => new KeyValuePair<string, string>(k, s));

                    return Enumerable.Empty<KeyValuePair<string, string>>();
                });
        }
    }
}