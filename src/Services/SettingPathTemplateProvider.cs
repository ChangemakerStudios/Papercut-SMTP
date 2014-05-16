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

namespace Papercut.Services
{
    using System.Collections.Generic;
    using System.Linq;

    using Papercut.Core.Configuration;
    using Papercut.Core.Events;
    using Papercut.Events;
    using Papercut.Properties;

    public class SettingPathTemplateProvider : IPathTemplatesProvider,
        IHandleEvent<SettingsUpdatedEvent>
    {
        public SettingPathTemplateProvider()
        {
            UpdatePathTemplates();
        }

        public void Handle(SettingsUpdatedEvent @event)
        {
            UpdatePathTemplates();
        }

        public IEnumerable<string> PathTemplates { get; private set; }

        void UpdatePathTemplates()
        {
            PathTemplates =
                Settings.Default.MessagePaths.Split(new[] { ';' })
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();
        }
    }
}