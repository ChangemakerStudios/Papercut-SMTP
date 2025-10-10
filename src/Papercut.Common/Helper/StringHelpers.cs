// Papercut
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


using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

using Papercut.Common.Extensions;

namespace Papercut.Common.Helper;

public static class StringHelpers
{
    static readonly Regex UpperCaseWordRegex = new("([A-Z]{1,1})[a-z]+", RegexOptions.Singleline);

    public static string SmallRandomString()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 6);
    }

    [StringFormatMethod("args")]
    public static string FormatWith(this string str, params object[] args)
    {
        return string.Format(str, args);
    }

    public static bool IsSet(this string? str)
    {
        return !string.IsNullOrWhiteSpace(str);
    }

    public static string Join(this IEnumerable<string>? strings, string separator)
    {
        return string.Join(separator, strings.IfNullEmpty());
    }

    public static string? ToTitleCase(this string? str, CultureInfo? culture = null)
    {
        if (str.IsNullOrWhiteSpace())
        {
            return str;
        }

        return (culture ?? Thread.CurrentThread.CurrentCulture).TextInfo.ToTitleCase(str);
    }

    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? str)
    {
        return string.IsNullOrWhiteSpace(str);
    }

    public static string CamelCaseToSeparated(this string str)
    {
        if (str.IsNullOrWhiteSpace())
        {
            return str;
        }

        var lines = new List<string>();
        var lastIndex = 0;
	
        foreach (var match in UpperCaseWordRegex.Matches(str).ToList())
        {
            if (match.Index > lastIndex)
            {
                lines.Add(str.Substring(lastIndex, match.Index - lastIndex).Trim());
            }

            lines.Add(match.Captures[0].Value);
            lastIndex = match.Index + match.Length + 1;
        }
	
        if (lastIndex < str.Length) {
            lines.Add(str.Substring(lastIndex, str.Length-lastIndex).Trim());
        }

        return string.Join(" ", lines);
    }

    public static string? Truncate([NotNullIfNotNull(nameof(input))] this string? input, int inputLimit, string? cutOff = "")
    {
        cutOff ??= string.Empty;

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

            // Finally, add the cut-off string...
            output += cutOff;
        }

        return output;
    }
}