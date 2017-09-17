// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2017 Jaben Cargman
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

namespace Papercut.Core.Domain.Paths
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Serilog;
    using Microsoft.Extensions.PlatformAbstractions;
    using System.Runtime.InteropServices;

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
                { "BaseDirectory", PlatformServices.Default.Application.ApplicationBasePath }
            };

            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            foreach (var specialFolder in SpecialFolder.BuiltinSpecialFolders)
            {
                string specialPathName = specialFolder.Name.ToString();
                if (!_templateDictionary.ContainsKey(specialPathName))
                    _templateDictionary.Add(specialPathName, isWindows ? specialFolder.WindowsPath : specialFolder.NonWindowsPath);
            }
        }

        public MessagePathConfigurator(IPathTemplatesProvider pathTemplateProvider, ILogger logger)
        {
            if (pathTemplateProvider == null) throw new ArgumentNullException(nameof(pathTemplateProvider));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            this._logger = logger;
            this._pathTemplateProvider = pathTemplateProvider;
            this._pathTemplateProvider.PathTemplates.CollectionChanged += this.PathTemplatesCollectionChanged;

            this.DefaultSavePath = PlatformServices.Default.Application.ApplicationBasePath;
            this.RenderLoadPaths();

            if (this.LoadPaths.Any()) this.DefaultSavePath = this.LoadPaths.First();

            this._logger.Information(
                "Default Message Save Path is Set to {DefaultSavePath}",
                this.DefaultSavePath);
        }

        public string DefaultSavePath
        {
            get
            {
                if (!Directory.Exists(this._defaultSavePath))
                {
                    this._logger.Information(
                        "Creating Default Message Save Path {DefaultSavePath} because it does not exist",
                        this._defaultSavePath);

                    Directory.CreateDirectory(this._defaultSavePath);
                }

                return this._defaultSavePath;
            }
            private set { this._defaultSavePath = value; }
        }

        public IEnumerable<string> LoadPaths { get; private set; }

        public event EventHandler RefreshLoadPath;

        void PathTemplatesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.RenderLoadPaths();
            this.OnRefreshLoadPath();
        }

        void RenderLoadPaths()
        {
            this.LoadPaths =
                this._pathTemplateProvider.PathTemplates.Select(this.RenderPathTemplate)
                    .Where(this.ValidatePathExists)
                    .ToList();

            this._logger.Information("Loading Messages from the Following Path(s) {@LoadPaths}", this.LoadPaths);
        }

        protected virtual void OnRefreshLoadPath()
        {
            EventHandler handler = this.RefreshLoadPath;
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
                    renderedPath = renderedPath.Replace($"%{pathKeyName}%", path).Replace(@"\\", @"\");
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
                this._logger.Error(ex, "Failure accessing or creating directory {DirectoryPath}", path);
            }

            return false;
        }


        class SpecialFolder
        {
            public static SpecialFolder[] BuiltinSpecialFolders = new[]
            {
                new SpecialFolder{
                    Name = FolderName.UserProfile,
                    WindowsPath =  Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%"),
                    NonWindowsPath = Environment.GetEnvironmentVariable("HOME")
                },
                new SpecialFolder{
                    Name = FolderName.ApplicationData,
                    WindowsPath =  Environment.GetEnvironmentVariable("APPDATA"),
                    NonWindowsPath = Environment.ExpandEnvironmentVariables("%HOME%/.config")
                },
                new SpecialFolder{
                    Name = FolderName.CommonApplicationData,
                    WindowsPath =  Environment.GetEnvironmentVariable("PROGRAMDATA"),
                    NonWindowsPath = "/usr/share"
                },
                new SpecialFolder{
                    Name = FolderName.Desktop,
                    WindowsPath =  Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%\\Desktop"),
                    NonWindowsPath = Environment.ExpandEnvironmentVariables("%HOME%/Desktop")
                },
            };


            public string WindowsPath { get; set; }
            public string NonWindowsPath { get; set; }
            public FolderName Name { get; set; }


            public enum FolderName
            {
                UserProfile,
                ApplicationData,
                CommonApplicationData,
                Desktop
            }
        }
    }
}