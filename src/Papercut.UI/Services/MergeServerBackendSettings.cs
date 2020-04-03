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

namespace Papercut.Services
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Papercut.Common.Domain;
    using Papercut.Common.Extensions;
    using Papercut.Core.Annotations;
    using Papercut.Core.Domain.Paths;
    using Papercut.Core.Infrastructure.Network;
    using Papercut.Events;
    using Papercut.Properties;

    public class MergeServerBackendSettings : IEventHandler<AppProcessExchangeEvent>
    {
        readonly IMessagePathConfigurator _configurator;

        readonly IMessageBus _messageBus;

        public MergeServerBackendSettings(IMessagePathConfigurator configurator, IMessageBus messageBus)
        {
            _configurator = configurator;
            this._messageBus = messageBus;
        }

        public void Handle([NotNull] AppProcessExchangeEvent @event)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));

            if (string.IsNullOrWhiteSpace(@event.MessageWritePath))
                return;

            if (!_configurator.LoadPaths.Any(
                s => s.StartsWith(@event.MessageWritePath, StringComparison.OrdinalIgnoreCase)))
            {
                // add it for watching...
                Settings.Default.MessagePaths = $"{Settings.Default.MessagePaths};{@event.MessageWritePath}";
            }

            var previousSettings = new Settings();

            Settings.Default.CopyTo(previousSettings);

            // save ip:port bindings as our own to keep in sync...
            Settings.Default.IP = @event.IP;
            Settings.Default.Port = @event.Port;
            Settings.Default.Save();

            this._messageBus.Publish(new SettingsUpdatedEvent(previousSettings));
        }
    }
}