// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
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
    private readonly ILogger _logger;

    private readonly IPathTemplatesProvider _pathTemplateProvider;

    private readonly string _defaultSavePath = string.Empty;

    protected PathConfiguratorBase(IPathTemplatesProvider pathTemplateProvider, ILogger logger)
    {
        if (pathTemplateProvider == null) throw new ArgumentNullException(nameof(pathTemplateProvider));
        if (logger == null) throw new ArgumentNullException(nameof(logger));

        _pathTemplateProvider = pathTemplateProvider;
        _pathTemplateProvider.PathTemplates.CollectionChanged += PathTemplatesCollectionChanged;

        _logger = logger.ForContext("PathType", _pathTemplateProvider.Type);

        var paths = RenderLoadPaths().ToList();
        DefaultSavePath = GetValidDefaultSavePath(paths);

        _logger.Information(
            "Default Save Path is Set to {DefaultSavePath}",
            DefaultSavePath);

        LoadPaths = paths.Where(PathExists).ToArray();

        _logger.Information("Loading from the Following Path(s) {@LoadPaths}", LoadPaths);
    }

    public string DefaultSavePath
    {
        get
        {
            if (Directory.Exists(_defaultSavePath)) return _defaultSavePath;

            _logger.Information(
                "Creating Default Save Path '{DefaultSavePath}' because it does not exist",
                _defaultSavePath);

            Directory.CreateDirectory(_defaultSavePath);

            return _defaultSavePath;
        }
        private init => _defaultSavePath = value;
    }

    public IReadOnlyCollection<string> LoadPaths { get; }

    public event EventHandler? RefreshLoadPath;

    private string GetValidDefaultSavePath(IEnumerable<string> possiblePaths)
    {
        foreach (var path in possiblePaths.Append(GetDefaultSavePath()))
        {
            if (IsSavePathIsValid(path))
            {
                return path;
            }

            // no permission -- moving on...
        }

        throw new NoValidSavePathFoundException($"Papercut SMTP does not have access to any paths for {_pathTemplateProvider.Type}");
    }

    private static string GetDefaultSavePath()
    {
        return RenderPathTemplate("%BaseDirectory%");
    }

    private bool IsSavePathIsValid(string defaultSavePath)
    {
        if (Directory.Exists(defaultSavePath)) return true;

        _logger.Information(
            "Attempting to Create Default Save Path '{DefaultSavePath}' because it does not exist",
            defaultSavePath);

        try
        {
            Directory.CreateDirectory(defaultSavePath);

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failure accessing path: {DirectoryPath}", defaultSavePath);
        }

        return false;
    }

    private void PathTemplatesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RenderLoadPaths();
        OnRefreshLoadPath();
    }

    private IEnumerable<string> RenderLoadPaths()
    {
        return
            _pathTemplateProvider.PathTemplates.Select(RenderPathTemplate)
                .ToList();
    }

    protected virtual void OnRefreshLoadPath()
    {
        EventHandler handler = RefreshLoadPath;
        handler?.Invoke(this, EventArgs.Empty);
    }

    private static string RenderPathTemplate(string pathTemplate)
    {
        return PathTemplateHelper.RenderPathTemplate(pathTemplate);
    }

    private bool PathExists(string path)
    {
        if (path == null) throw new ArgumentNullException(nameof(path));

        try
        {
            return Directory.Exists(path);
        }
        catch (Exception ex)
        {
            _logger.Information(ex, "Excluding search path {DirectoryPath} since there is no access to it", path);
        }

        return false;
    }
}