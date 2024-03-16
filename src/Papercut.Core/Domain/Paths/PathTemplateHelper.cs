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


using System.Text.RegularExpressions;

using Papercut.Common.Helper;

namespace Papercut.Core.Domain.Paths
{
    public class PathTemplateHelper
    {
        static readonly IDictionary<string, string> _templateDictionary;

        static readonly Regex TemplateRegex = new Regex(
            @"\%(?<name>.+?)\%",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline);

        static PathTemplateHelper()
        {
            _templateDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    nameof(AppDomain.CurrentDomain.BaseDirectory),
                    AppDomain.CurrentDomain.BaseDirectory
                },
                { "DataDirectory", AppConstants.AppDataDirectory },
                { nameof(AppConstants.AppDataDirectory), AppConstants.AppDataDirectory },
                { nameof(AppConstants.UserAppDataDirectory), AppConstants.UserAppDataDirectory }
            };

            foreach (Environment.SpecialFolder specialPath in EnumHelpers.GetEnumList<Environment.SpecialFolder>())
            {
                string specialPathName = specialPath.ToString();

                if (!_templateDictionary.ContainsKey(specialPathName)) _templateDictionary.Add(specialPathName, Environment.GetFolderPath(specialPath));
            }
        }

        public static string RenderPathTemplate(string pathTemplate)
        {
            var pathKeys =
                TemplateRegex.Matches(pathTemplate)
                    .OfType<Match>()
                    .Select(s => s.Groups["name"].Value);

            string renderedPath = pathTemplate.Trim();

            bool isUncPath = renderedPath.StartsWith(@"\\");

            if (isUncPath)
            {
                // remove \\ from start of path
                renderedPath = renderedPath.Substring(2, renderedPath.Length - 2);
            }

            foreach (string pathKeyName in pathKeys)
            {
                if (_templateDictionary.TryGetValue(pathKeyName, out var path))
                {
                    renderedPath = renderedPath.Replace($"%{pathKeyName}%", path).Replace(@"\\", @"\");
                }
            }

            return isUncPath ? $@"\\{renderedPath}" : renderedPath;
        }
    }
}