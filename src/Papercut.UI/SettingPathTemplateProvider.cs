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


using System.Collections.ObjectModel;

using Papercut.Core.Domain.Paths;

namespace Papercut;

public class SettingPathTemplateProvider : IPathTemplatesProvider,
    IEventHandler<SettingsUpdatedEvent>
{
    private readonly ILogger _logger;

    public SettingPathTemplateProvider(PathTemplateType type, ILogger logger)
    {
        Type = type;
        _logger = logger.ForContext("PathType", type);


        PathTemplates = new ObservableCollection<string>(Paths);
    }

    private IEnumerable<string> Paths => Type switch
    {
        PathTemplateType.Message => SplitPaths(GetMessagePath()),
        PathTemplateType.Logging => SplitPaths(GetLoggingPath()),
        _ => Enumerable.Empty<string>()
    };

    public Task HandleAsync(SettingsUpdatedEvent @event, CancellationToken token)
    {
        UpdatePathTemplate(PathTemplates, Paths);

        return Task.CompletedTask;
    }

    public PathTemplateType Type { get; }

    public ObservableCollection<string> PathTemplates { get; }

    private IEnumerable<string> SplitPaths(string paths)
    {
        return paths
            .Split(';', ',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct();
    }

    private string GetLoggingPath()
    {
        try
        {
            return Settings.Default.LoggingPaths;
        }
        catch (Exception ex)
        {
            // logging path loading is failing
            _logger.Error(ex, "Failed to load logging paths");

            // use default
            return @"%ApplicationData%\Changemaker Studios\Papercut SMTP;%ApplicationData%\Papercut;%BaseDirectory%\Logs;%DataDirectory%\Logs";
        }
    }

    private string GetMessagePath()
    {
        try
        {
            return Settings.Default.MessagePaths;
        }
        catch (Exception ex)
        {
            // message path loading is failing
            _logger.Error(ex, "Failed to load message paths");

            // use default
            return @"%ApplicationData%\Changemaker Studios\Papercut SMTP;%ApplicationData%\Papercut;%BaseDirectory%\Incoming;%DataDirectory%\Incoming";
        }
    }

    private void UpdatePathTemplate(ICollection<string> pathTemplate, IEnumerable<string> changedPaths)
    {
        string[] paths = changedPaths.ToArray();
        var (toAdd, toRemove) = (paths.Except(pathTemplate).ToArray(), pathTemplate.Except(paths).ToArray());

        if (toRemove.Any())
        {
            _logger.Information("Removing {Type:l} Path Templates: {Paths}", Type, toRemove);
            pathTemplate.RemoveRange(toRemove);
        }

        if (toAdd.Any())
        {
            _logger.Information("Added {Type:l} Path Templates: {Paths}", Type, toAdd);
            pathTemplate.AddRange(toAdd);
        }
    }

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    private static void Register(ContainerBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.Register(p => new SettingPathTemplateProvider(PathTemplateType.Message, p.Resolve<ILogger>().ForContext<SettingPathTemplateProvider>()))
            .Keyed<IPathTemplatesProvider>(PathTemplateType.Message).SingleInstance();

        builder.Register(p => new SettingPathTemplateProvider(PathTemplateType.Logging, p.Resolve<ILogger>().ForContext<SettingPathTemplateProvider>()))
            .Keyed<IPathTemplatesProvider>(PathTemplateType.Logging).SingleInstance();
    }

    #endregion
}