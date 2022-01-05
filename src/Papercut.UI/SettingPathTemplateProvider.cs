// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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

namespace Papercut
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Papercut.Common.Domain;
    using Papercut.Common.Extensions;
    using Papercut.Core.Domain.Paths;
    using Papercut.Domain.Events;
    using Papercut.Properties;

    using Serilog;

    public class SettingPathTemplateProvider : IPathTemplatesProvider,
        IEventHandler<SettingsUpdatedEvent>
    {
        readonly ILogger _logger;

        public SettingPathTemplateProvider(ILogger logger)
        {
            _logger = logger;
            MessagePathTemplates = new ObservableCollection<string>(MessagePaths);
            LoggingPathTemplates = new ObservableCollection<string>(LoggingPaths);
        }

        private IEnumerable<string> LoggingPaths
        {
            get
            {
                return GetLoggingPath()
                    .Split(';', ',')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct();
            }
        }

        private string GetLoggingPath()
        {
            try
            {
                return Settings.Default.LoggingPaths;
            }
            catch (System.Exception ex)
            {
                // logging path loading is failing
                this._logger.Error(ex, "Failed to load logging paths");

                // use default
                return @"%ApplicationData%\Changemaker Studios\Papercut SMTP;%ApplicationData%\Papercut;%BaseDirectory%\Logs;%DataDirectory%\Logs";
            }
        }

        private IEnumerable<string> MessagePaths
        {
            get
            {
                return GetMessagePath()
                    .Split(';', ',')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct();
            }
        }

        private string GetMessagePath()
        {
            try
            {
                return Settings.Default.MessagePaths;
            }
            catch (System.Exception ex)
            {
                // message path loading is failing
                this._logger.Error(ex, "Failed to load message paths");

                // use default
                return @"%ApplicationData%\Changemaker Studios\Papercut SMTP;%ApplicationData%\Papercut;%BaseDirectory%\Incoming;%DataDirectory%\Incoming";
            }
        }

        public Task HandleAsync(SettingsUpdatedEvent @event, CancellationToken token)
        {
            UpdateMessagePathTemplates();
            UpdateLoggingPathTemplates();

            return Task.CompletedTask;
        }

        public ObservableCollection<string> MessagePathTemplates { get; }
        public ObservableCollection<string> LoggingPathTemplates { get; }

        void UpdateMessagePathTemplates()
        {
            string[] paths = MessagePaths.ToArray();
            List<string> toRemove = MessagePathTemplates.Except(paths).ToList();
            List<string> toAdd = paths.Except(MessagePathTemplates).ToList();

            if (toRemove.Any()) _logger.Information("Removing message Path(s) {Paths} to message Path Templates", toRemove);

            toRemove.ForEach(s => MessagePathTemplates.Remove(s));

            if (toAdd.Any()) _logger.Information("Added message Path(s) {Paths} to message Path Templates", toAdd);

            MessagePathTemplates.AddRange(toAdd);
        }

        void UpdateLoggingPathTemplates()
        {
            string[] paths = LoggingPaths.ToArray();
            List<string> toRemove = LoggingPathTemplates.Except(paths).ToList();
            List<string> toAdd = paths.Except(LoggingPathTemplates).ToList();

            if (toRemove.Any()) _logger.Information("Removing logging Path(s) {Paths} to logging Path Templates", toRemove);

            toRemove.ForEach(s => LoggingPathTemplates.Remove(s));

            if (toAdd.Any()) _logger.Information("Added logging Path(s) {Paths} to logging Path Templates", toAdd);

            LoggingPathTemplates.AddRange(toAdd);
        }
    }
}