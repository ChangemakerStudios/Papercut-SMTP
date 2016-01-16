// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2016 Jaben Cargman
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
        static readonly IDictionary<string, string> _templateDictionary;

        static readonly Regex _templateRegex = new Regex(
            @"\%(?<name>.+?)\%",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline);

        readonly ILogger _logger;

        readonly IPathTemplatesProvider _pathTemplateProvider;

        string _defaultSavePath;

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
            if (pathTemplateProvider == null) throw new ArgumentNullException(nameof(pathTemplateProvider));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _logger = logger;
            _pathTemplateProvider = pathTemplateProvider;
            _pathTemplateProvider.PathTemplates.CollectionChanged += PathTemplatesCollectionChanged;

            DefaultSavePath = AppDomain.CurrentDomain.BaseDirectory;
            RenderLoadPaths();

            if (LoadPaths.Any()) DefaultSavePath = LoadPaths.First();

            _logger.Information(
                "Default Message Save Path is Set to {DefaultSavePath}",
                DefaultSavePath);
        }

        public string DefaultSavePath
        {
            get
            {
                if (!Directory.Exists(_defaultSavePath))
                {
                    _logger.Information(
                        "Creating Default Message Save Path {DefaultSavePath} because it does not exist",
                        _defaultSavePath);

                    Directory.CreateDirectory(_defaultSavePath);
                }

                return _defaultSavePath;
            }
            private set { _defaultSavePath = value; }
        }

        public IEnumerable<string> LoadPaths { get; private set; }

        public event EventHandler RefreshLoadPath;

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

            _logger.Information("Loading Messages from the Following Path(s) {@LoadPaths}", LoadPaths);
        }

        protected virtual void OnRefreshLoadPath()
        {
            EventHandler handler = RefreshLoadPath;
            handler?.Invoke(this, EventArgs.Empty);
        }

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
                        renderedPath.Replace($"%{pathKeyName}%", path)
                            .Replace(@"\\", @"\");
                }
            }

            return renderedPath;
        }

        bool ValidatePathExists(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

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
    }
}