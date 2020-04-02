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
    using System.Text.RegularExpressions;

    public static class HtmlHelpers
    {
        private static readonly Regex _uriRegex = new Regex(
            @"(((?<scheme>http(s)?):\/\/)([\w-]+?\.\w+)+([a-zA-Z0-9\~\!\@\#\$\%\^\&amp\;\*\(\)_\-\=\+\\\/\?\.\:\;\,]*)?)",
            RegexOptions.Compiled | RegexOptions.Multiline);

        public static string Linkify(this string text, string target = "_self")
        {
            return _uriRegex.Replace(
                text,
                match =>
                {
                    try
                    {
                        var link = match.ToString();
                        var scheme = match.Groups["scheme"].Value == "https" ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;

                        var url = new UriBuilder(link) { Scheme = scheme }.Uri.ToString();

                        return $@"<a href=""{url}"" target=""{target}"">{link}</a>";
                    }
                    catch (Exception)
                    {
                        return match.ToString();
                    }
                }
            );
        }
    }
}