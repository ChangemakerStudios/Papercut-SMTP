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

namespace Papercut.Core.Domain.Paths;

public abstract class PathConfiguratorBase : IPathConfigurator
{
    readonly ILogger _logger;

    readonly IPathTemplatesProvider _pathTemplateProvider;

    readonly string _defaultSavePath;

    protected PathConfiguratorBase(IPathTemplatesProvider pathTemplateProvider, ILogger logger)
    {
        if (pathTemplateProvider == null) throw new ArgumentNullException(nameof(pathTemplateProvider));
        if (logger == null) throw new ArgumentNullException(nameof(logger));

        this._logger = logger;
        this._pathTemplateProvider = pathTemplateProvider;
        this._pathTemplateProvider.MessagePathTemplates.CollectionChanged += this.PathTemplatesCollectionChanged;

        var paths = this.RenderLoadPaths().ToList();
        this.DefaultSavePath = this.GetValidDefaultSavePath(paths);

        this._logger.Information(
            "Default Save Path is Set to {DefaultSavePath}",
            this.DefaultSavePath);

        this.LoadPaths = paths.Where(this.PathExists).ToArray();

        this._logger.Information("Loading from the Following Path(s) {@LoadPaths}", this.LoadPaths);
    }

    public string DefaultSavePath
    {
        get
        {
            if (Directory.Exists(this._defaultSavePath)) return this._defaultSavePath;

            this._logger.Information(
                "Creating Default Save Path '{DefaultSavePath}' because it does not exist",
                this._defaultSavePath);

            Directory.CreateDirectory(this._defaultSavePath);

            return this._defaultSavePath;
        }
        private init => this._defaultSavePath = value;
    }

    public IReadOnlyCollection<string> LoadPaths { get; }

    public event EventHandler RefreshLoadPath;

    private string GetValidDefaultSavePath(IEnumerable<string> possiblePaths)
    {
        foreach (var path in possiblePaths.Append(GetDefaultSavePath()))
        {
            if (this.IsSavePathIsValid(path))
            {
                return path;
            }

            // no permission -- moving on...
        }

        throw new NoValidSavePathFoundException("Papercut SMTP does not have access to any paths to save emails!");
    }

    static string GetDefaultSavePath()
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        if (baseDirectory.EndsWith("current", StringComparison.OrdinalIgnoreCase))
        {
            // Velo installation -- nothing should go in the "current" directory
            return Path.GetDirectoryName(baseDirectory)!;
        }

        return baseDirectory;
    }

    bool IsSavePathIsValid(string defaultSavePath)
    {
        if (Directory.Exists(defaultSavePath)) return true;

        this._logger.Information(
            "Attempting to Create Default Save Path '{DefaultSavePath}' because it does not exist",
            defaultSavePath);

        try
        {
            Directory.CreateDirectory(defaultSavePath);

            return true;
        }
        catch (Exception ex)
        {
            this._logger.Error(ex, "Failure accessing path: {DirectoryPath}", defaultSavePath);
        }

        return false;
    }

    void PathTemplatesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        this.RenderLoadPaths();
        this.OnRefreshLoadPath();
    }

    IEnumerable<string> RenderLoadPaths()
    {
        return
            this._pathTemplateProvider.MessagePathTemplates.Select(this.RenderPathTemplate)
                .ToList();
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

    bool PathExists(string path)
    {
        if (path == null) throw new ArgumentNullException(nameof(path));

        try
        {
            return Directory.Exists(path);
        }
        catch (Exception ex)
        {
            this._logger.Information(ex, "Excluding search path {DirectoryPath} since there is no access to it", path);
        }

        return false;
    }
}