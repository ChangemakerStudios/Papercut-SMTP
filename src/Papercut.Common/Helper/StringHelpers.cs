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
    using System.Globalization;
    using System.Threading;

    using Papercut.Common.Extensions;
    using Papercut.Core.Annotations;

    public static class StringHelpers
    {
        public static string SmallRandomString()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 6);
        }

        [StringFormatMethod("args")]
        public static string FormatWith(this string str, params object[] args)
        {
            return string.Format(str, args);
        }

        public static bool IsSet([CanBeNull] this string str)
        {
            return !string.IsNullOrWhiteSpace(str);
        }

        public static string Join([CanBeNull] this IEnumerable<string> strings, string seperator)
        {
            return string.Join(seperator, strings.IfNullEmpty());
        }

        public static string ToTitleCase([CanBeNull] this string str, CultureInfo culture = null)
        {
            if (str.IsNullOrWhiteSpace())
            {
                return str;
            }

            return (culture ?? Thread.CurrentThread.CurrentCulture).TextInfo.ToTitleCase(str);
        }

        public static bool IsNullOrWhiteSpace([CanBeNull] this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        public static string Truncate([CanBeNull] this string input, int inputLimit, [CanBeNull] string cutOff = "...")
        {
            cutOff = cutOff ?? string.Empty;

            if (string.IsNullOrWhiteSpace(input)) return null;

            string output = input;
            int limit = inputLimit - cutOff.Length;

            // Check if the string is longer than the allowed amount
            // otherwise do nothing
            if (output.Length > limit && limit > 0)
            {
                // cut the string down to the maximum number of characters
                output = output.Substring(0, limit);

                // Check if the space right after the truncate point
                // was a space. if not, we are in the middle of a word and
                // need to cut out the rest of it
                if (input.Substring(output.Length, 1) != " ")
                {
                    int lastSpace = output.LastIndexOf(" ");

                    // if we found a space then, cut back to that space
                    if (lastSpace != -1) output = output.Substring(0, lastSpace);
                }

                // Finally, add the the cut off string...
                output += cutOff;
            }

            return output;
        }
    }
}