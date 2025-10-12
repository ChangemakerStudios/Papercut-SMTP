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
    readonly ILogger _logger;

    public SettingPathTemplateProvider(ILogger logger)
    {
        this._logger = logger;
        this.MessagePathTemplates = new ObservableCollection<string>(this.MessagePaths);
        this.LoggingPathTemplates = new ObservableCollection<string>(this.LoggingPaths);
    }

    private IEnumerable<string> LoggingPaths => this.SplitPaths(this.GetLoggingPath());

    private IEnumerable<string> MessagePaths => this.SplitPaths(this.GetMessagePath());

    public Task HandleAsync(SettingsUpdatedEvent @event, CancellationToken token)
    {
        this.UpdatePathTemplate(this.MessagePathTemplates, this.MessagePaths, "Message");
        this.UpdatePathTemplate(this.LoggingPathTemplates, this.LoggingPaths, "Logging");

        return Task.CompletedTask;
    }

    public ObservableCollection<string> MessagePathTemplates { get; }

    public ObservableCollection<string> LoggingPathTemplates { get; }

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
            this._logger.Error(ex, "Failed to load logging paths");

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
            this._logger.Error(ex, "Failed to load message paths");

            // use default
            return @"%ApplicationData%\Changemaker Studios\Papercut SMTP;%ApplicationData%\Papercut;%BaseDirectory%\Incoming;%DataDirectory%\Incoming";
        }
    }

    void UpdatePathTemplate(ICollection<string> pathTemplate, IEnumerable<string> changedPaths, string name)
    {
        string[] paths = changedPaths.ToArray();
        var (toAdd, toRemove) = (paths.Except(pathTemplate).ToArray(), pathTemplate.Except(paths).ToArray());

        if (toRemove.Any())
        {
            this._logger.Information("Removing {Type:l} Path Templates: {Paths}", name, toRemove);
            pathTemplate.RemoveRange(toRemove);
        }

        if (toAdd.Any())
        {
            this._logger.Information("Added {Type:l} Path Templates: {Paths}", name, toAdd);
            pathTemplate.AddRange(toAdd);
        }
    }
}