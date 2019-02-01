// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2019 Jaben Cargman
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
    using System.Text.RegularExpressions;

    public static class HtmlHelpers
    {
        private static readonly Regex uriRegEx = new Regex(
            @"\b(((\S+)?)(@|mailto\:|(news|(ht|f)tp(s?))\://)\S+\/?)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// this will find links like: http://www.mysite.com
        /// as well as any links with other characters directly in front of it like: href="http://www.mysite.com"
        ///
        /// Credit goes to:
        /// https://stackoverflow.com/questions/758135/c-sharp-code-to-linkify-urls-in-a-string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Linkify(this string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return str;

            foreach (Match match in uriRegEx.Matches(str))
            {
                if (match.Value.StartsWith("http"))
                {
                    str = str.Replace(match.Value, $"<a href='{match.Value}'>{match.Value}</a>");
                }
            }

            return str;
        }
    }
}