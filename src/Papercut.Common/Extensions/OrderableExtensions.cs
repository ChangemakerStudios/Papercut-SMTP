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


using Papercut.Common.Domain;

namespace Papercut.Common.Extensions
{
    public static class OrderableExtensions
    {
        public static IReadOnlyCollection<T> MaybeByOrderable<T>(this IEnumerable<T> items)
        {
            return items.IfNullEmpty().Distinct()
                .Select((e, i) => new { Index = 100 + i, Item = e }).OrderBy(
                    e =>
                    {
                        var orderable = e.Item as IOrderable;
                        return orderable?.Order ?? e.Index;
                    }).Select(e => e.Item).ToReadOnlyCollection();
        }
    }
}