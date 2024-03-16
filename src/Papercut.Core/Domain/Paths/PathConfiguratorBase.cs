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


using System.Collections.Specialized;

namespace Papercut.Core.Domain.Paths
{
    public abstract class PathConfiguratorBase : IPathConfigurator
    {
        readonly ILogger _logger;

        readonly IPathTemplatesProvider _pathTemplateProvider;

        string _defaultSavePath;

        protected PathConfiguratorBase(IPathTemplatesProvider pathTemplateProvider, ILogger logger)
        {
            if (pathTemplateProvider == null) throw new ArgumentNullException(nameof(pathTemplateProvider));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            this._logger = logger;
            this._pathTemplateProvider = pathTemplateProvider;
            this._pathTemplateProvider.MessagePathTemplates.CollectionChanged += this.PathTemplatesCollectionChanged;

            this.DefaultSavePath = AppDomain.CurrentDomain.BaseDirectory;
            this.RenderLoadPaths();

            if (this.LoadPaths.Any()) this.DefaultSavePath = this.LoadPaths.First();

            this._logger.Information(
                "Default Save Path is Set to {DefaultSavePath}",
                this.DefaultSavePath);
        }

        public string DefaultSavePath
        {
            get
            {
                if (!Directory.Exists(this._defaultSavePath))
                {
                    this._logger.Information(
                        "Creating Default Save Path {DefaultSavePath} because it does not exist",
                        this._defaultSavePath);

                    Directory.CreateDirectory(this._defaultSavePath);
                }

                return this._defaultSavePath;
            }
            private set => this._defaultSavePath = value;
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
                this._pathTemplateProvider.MessagePathTemplates.Select(this.RenderPathTemplate)
                    .Where(this.ValidatePathExists)
                    .ToList();

            this._logger.Information("Loading from the Following Path(s) {@LoadPaths}", this.LoadPaths);
        }

        protected virtual void OnRefreshLoadPath()
        {
            EventHandler handler = this.RefreshLoadPath;
            handler?.Invoke(this, EventArgs.Empty);
        }

        string RenderPathTemplate(string pathTemplate)
        {
            return PathTemplateHelper.RenderPathTemplate(pathTemplate);
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
    }
}