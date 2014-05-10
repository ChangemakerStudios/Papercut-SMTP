/*  
 * Papercut
 *
 *  Copyright © 2008 - 2012 Ken Robertson
 *  Copyright © 2013 - 2014 Jaben Cargman
 *  
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *  
 *  http://www.apache.org/licenses/LICENSE-2.0
 *  
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *  
 */

namespace Papercut.Core.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Principal;
    using System.Text.RegularExpressions;

    public class MessagePathConfigurator : IMessagePathConfigurator
    {
        #region Static Fields

        static readonly IDictionary<string, string> _templateDictionary = null;

        static readonly Regex _templateRegex = new Regex(
            @"\%(?<name>.+?)\%",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline);

        #endregion

        #region Constructors and Destructors

        static MessagePathConfigurator()
        {
            _templateDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "BaseDirectory",
                    AppDomain.CurrentDomain.BaseDirectory
                }
            };

            foreach (var specialPath in GeneralExtensions.EnumAsList<Environment.SpecialFolder>())
            {
                var specialPathName = specialPath.ToString();

                if (!_templateDictionary.ContainsKey(specialPathName))
                {
                    _templateDictionary.Add(specialPathName, Environment.GetFolderPath(specialPath));
                }
            }
        }

        public MessagePathConfigurator(IPathTemplatesProvider pathTemplateProvider)
        {
            if (pathTemplateProvider == null)
            {
                throw new ArgumentNullException("pathTemplateProvider");
            }

            DefaultSavePath = AppDomain.CurrentDomain.BaseDirectory;

            LoadPaths = pathTemplateProvider.PathTemplates
                .Select(RenderPathTemplate)
                .Where(ValidatePathExists)
                .ToList();

            bool isSystem;
            using (var identity = WindowsIdentity.GetCurrent()) isSystem = identity.IsSystem;

            if (!isSystem && LoadPaths.Any())
            {
                DefaultSavePath = LoadPaths.First();
            }
        }

        #endregion

        #region Public Properties

        public string DefaultSavePath { get; private set; }

        public IEnumerable<string> LoadPaths { get; private set; }

        #endregion

        #region Methods

        string RenderPathTemplate(string pathTemplate)
        {
            var pathKeys = _templateRegex.Matches(pathTemplate).OfType<Match>().Select(s => s.Groups["name"].Value);
            var renderedPath = pathTemplate;

            foreach (var pathKeyName in pathKeys)
            {
                string path;
                if (_templateDictionary.TryGetValue(pathKeyName, out path))
                {
                    renderedPath = renderedPath.Replace(string.Format("%{0}%", pathKeyName), path).Replace(@"\\", @"\");
                }
            }

            return renderedPath;
        }

        bool ValidatePathExists(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteError(string.Format("Failure accessing or creating directory: {0}", path), ex);
            }

            return false;
        }

        #endregion
    }
}