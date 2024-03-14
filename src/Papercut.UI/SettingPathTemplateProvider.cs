// Papercut SMTP
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

using System.Collections.ObjectModel;

using DynamicData;

using Papercut.Common.Domain;
using Papercut.Core.Domain.Paths;
using Papercut.Events;
using Papercut.Properties;

namespace Papercut;

public class SettingPathTemplateProvider : IPathTemplatesProvider,
    IEventHandler<SettingsUpdatedEvent>
{
    private readonly ILogger _logger;

    public SettingPathTemplateProvider(ILogger logger)
    {
        this._logger = logger;
        this.PathTemplates = new ObservableCollection<string>(this.MessagePaths);
    }

    private IEnumerable<string> MessagePaths
    {
        get
        {
            return this.GetMessagePath()
                .Split(';', ',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct();
        }
    }

    public void Handle(SettingsUpdatedEvent @event)
    {
        this.UpdatePathTemplates();
    }

    public ObservableCollection<string> PathTemplates { get; }

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
            return "%ApplicationData%\\Papercut;%BaseDirectory%;%BaseDirectory%\\Incoming";
        }
    }

    private void UpdatePathTemplates()
    {
        var paths = this.MessagePaths.ToArray();
        var toRemove = this.PathTemplates.Except(paths).ToList();
        var toAdd = paths.Except(this.PathTemplates).ToList();

        if (toRemove.Any()) this._logger.Information("Removing Path(s) {Paths} to Path Templates", toRemove);

        toRemove.ForEach(s => this.PathTemplates.Remove(s));

        if (toAdd.Any()) this._logger.Information("Added Path(s) {Paths} to Path Templates", toAdd);

        this.PathTemplates.AddRange(toAdd);
    }
}