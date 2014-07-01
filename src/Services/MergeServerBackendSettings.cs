// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2014 Jaben Cargman
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

namespace Papercut.Services
{
    using System;
    using System.Linq;

    using Papercut.Core.Configuration;
    using Papercut.Core.Events;
    using Papercut.Events;
    using Papercut.Properties;

    public class MergeServerBackendSettings : IHandleEvent<AppProcessExchangeEvent>
    {
        readonly IMessagePathConfigurator _configurator;

        readonly IPublishEvent _publishEvent;

        public MergeServerBackendSettings(
            IMessagePathConfigurator configurator,
            IPublishEvent publishEvent)
        {
            _configurator = configurator;
            _publishEvent = publishEvent;
        }

        public void Handle(AppProcessExchangeEvent @event)
        {
            if (string.IsNullOrWhiteSpace(@event.MessageWritePath)) return;

            if (
                !_configurator.LoadPaths.Any(
                    s => s.StartsWith(@event.MessageWritePath, StringComparison.OrdinalIgnoreCase)))
            {
                // add it for watching...
                Settings.Default.MessagePaths = string.Format(
                    "{0};{1}",
                    Settings.Default.MessagePaths,
                    @event.MessageWritePath);
            }

            // save ip:port bindings as our own to keep in sync...
            Settings.Default.IP = @event.IP;
            Settings.Default.Port = @event.Port;
            Settings.Default.Save();

            _publishEvent.Publish(new SettingsUpdatedEvent());
        }
    }
}