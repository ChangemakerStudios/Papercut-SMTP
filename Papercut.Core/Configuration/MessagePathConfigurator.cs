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
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Security.Principal;
    using System.Text.RegularExpressions;

    using Papercut.Core.Helper;

    using Serilog;

    public class MessagePathConfigurator : IMessagePathConfigurator
    {
        readonly IPathTemplatesProvider _pathTemplateProvider;

        readonly ILogger _logger;

        #region Static Fields

        static readonly IDictionary<string, string> _templateDictionary;

        static readonly Regex _templateRegex = new Regex(
            @"\%(?<name>.+?)\%",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline);

        #endregion

        #region Constructors and Destructors

        static MessagePathConfigurator()
        {
            _templateDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "BaseDirectory", AppDomain.CurrentDomain.BaseDirectory }
            };

            foreach (
                Environment.SpecialFolder specialPath in
                    GeneralExtensions.EnumAsList<Environment.SpecialFolder>())
            {
                string specialPathName = specialPath.ToString();

                if (!_templateDictionary.ContainsKey(specialPathName)) _templateDictionary.Add(specialPathName, Environment.GetFolderPath(specialPath));
            }
        }

        public MessagePathConfigurator(IPathTemplatesProvider pathTemplateProvider, ILogger logger)
        {
            if (pathTemplateProvider == null) throw new ArgumentNullException("pathTemplateProvider");
            if (logger == null) throw new ArgumentNullException("logger");

            _logger = logger;
            _pathTemplateProvider = pathTemplateProvider;
            _pathTemplateProvider.PathTemplates.CollectionChanged += PathTemplatesCollectionChanged;

            DefaultSavePath = AppDomain.CurrentDomain.BaseDirectory;
            RenderLoadPaths();

            bool isSystem;
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent()) isSystem = identity.IsSystem;

            if (!isSystem && LoadPaths.Any()) DefaultSavePath = LoadPaths.First();
        }

        void PathTemplatesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RenderLoadPaths();
            OnRefreshLoadPath();
        }

        void RenderLoadPaths()
        {
            LoadPaths =
                _pathTemplateProvider.PathTemplates.Select(RenderPathTemplate)
                    .Where(ValidatePathExists)
                    .ToList();
        }

        #endregion

        #region Public Properties

        public string DefaultSavePath { get; private set; }

        public IEnumerable<string> LoadPaths { get; private set; }

        public event EventHandler RefreshLoadPath;

        protected virtual void OnRefreshLoadPath()
        {
            EventHandler handler = RefreshLoadPath;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        #endregion

        #region Methods

        string RenderPathTemplate(string pathTemplate)
        {
            IEnumerable<string> pathKeys =
                _templateRegex.Matches(pathTemplate)
                    .OfType<Match>()
                    .Select(s => s.Groups["name"].Value);
            string renderedPath = pathTemplate;

            foreach (string pathKeyName in pathKeys)
            {
                string path;
                if (_templateDictionary.TryGetValue(pathKeyName, out path))
                {
                    renderedPath =
                        renderedPath.Replace(string.Format("%{0}%", pathKeyName), path)
                            .Replace(@"\\", @"\");
                }
            }

            return renderedPath;
        }

        bool ValidatePathExists(string path)
        {
            if (path == null) throw new ArgumentNullException("path");

            try
            {
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failure accessing or creating directory {DirectoryPath}", path);
            }

            return false;
        }

        #endregion
    }
}